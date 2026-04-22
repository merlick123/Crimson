using System.Text.Json;
using Crimson.Core.Generation;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Generation.Cpp;
using Crimson.Core.Generation.Rust;
using Crimson.Core.Host;
using Crimson.Core.Merge;
using Crimson.Core.Model;
using Crimson.Core.Parsing;
using Crimson.Core.Projects;
using Crimson.Core.Validation;

namespace Crimson.Core;

public sealed class CrimsonWorkspace
{
    private readonly CrimsonCompiler _compiler = new();
    private readonly TreeMergeEngine _mergeEngine = new();
    private readonly CrimsonValidator _validator = new();
    private readonly IReadOnlyDictionary<string, ITargetEmitter> _emitters;
    private readonly IReadOnlyDictionary<string, IHostIntegration> _hostIntegrations;
    private readonly IReadOnlyDictionary<string, IProjectInitProfile> _initProfiles;

    public CrimsonWorkspace(
        IEnumerable<ITargetEmitter>? emitters = null,
        IEnumerable<IHostIntegration>? hostIntegrations = null,
        IEnumerable<IProjectInitProfile>? initProfiles = null)
    {
        var configuredEmitters = (emitters ?? [new CSharpTargetEmitter(), new CppTargetEmitter(), new RustTargetEmitter()])
            .ToArray();
        var configuredHostIntegrations = (hostIntegrations ?? [new DotNetMsbuildHostIntegration(), new CMakeHostIntegration(), new CargoHostIntegration()])
            .ToArray();
        var configuredProfiles = (initProfiles ?? [
            new CSharpProjectInitProfile(),
            new CppCMakeProjectInitProfile(),
            new CppCMakeGccProjectInitProfile(),
            new CppCMakeCrossProjectInitProfile(),
            new RustCargoProjectInitProfile(),
            new RustCargoNoStdProjectInitProfile(),
        ])
            .ToArray();

        _emitters = configuredEmitters
            .GroupBy(static emitter => emitter.TargetName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"Multiple target emitters were registered for '{group.Key}'."),
                StringComparer.OrdinalIgnoreCase);

        _hostIntegrations = configuredHostIntegrations
            .GroupBy(static hostIntegration => hostIntegration.HostName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"Multiple host integrations were registered for '{group.Key}'."),
                StringComparer.OrdinalIgnoreCase);

        _initProfiles = configuredProfiles
            .GroupBy(static profile => profile.ProfileId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"Multiple init profiles were registered for '{group.Key}'."),
                StringComparer.OrdinalIgnoreCase);
    }

    public CompilationUnitModel ParseFile(string filePath) =>
        _compiler.ParseFile(filePath);

    public CompilationSetModel ParseFiles(IEnumerable<string> filePaths) =>
        _compiler.ParseFiles(filePaths);

    public CompilationSetModel ValidateFiles(IEnumerable<string> filePaths)
    {
        var compilation = ParseFiles(filePaths);
        _validator.Validate(compilation);
        return compilation;
    }

    public string EmitAstJson(CompilationUnitModel model) =>
        JsonSerializer.Serialize(model, JsonDefaults.Options);

    public IReadOnlyList<ProjectInitProfileInfo> GetInitProfiles() =>
        _initProfiles.Values
            .OrderBy(static profile => profile.ProfileId, StringComparer.OrdinalIgnoreCase)
            .Select(static profile => new ProjectInitProfileInfo(profile.ProfileId, profile.DisplayName, profile.Description))
            .ToArray();

    public void InitProject(string projectFilePath, string profileId, bool starter)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Project directory not found.");
        if (!_initProfiles.TryGetValue(profileId, out var profile))
        {
            throw new InvalidOperationException($"Unknown init profile '{profileId}'. Run 'crimson init-profiles' to list available profiles.");
        }

        var projectName = Path.GetFileNameWithoutExtension(fullPath);
        var plan = profile.CreatePlan(new ProjectInitContext(fullPath, directory, projectName, starter));
        Directory.CreateDirectory(directory);

        var configuredGroups = plan.Groups
            .GroupBy(static group => group.GroupName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"Init profile '{profileId}' configured group '{group.Key}' multiple times."),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in configuredGroups.Values)
        {
            if (!_emitters.ContainsKey(group.Kind))
            {
                throw new InvalidOperationException($"Init profile '{profileId}' requires emitter '{group.Kind}', but no emitter is registered for it.");
            }

            if (group.Host is not null && !_hostIntegrations.ContainsKey(group.Host.HostName))
            {
                throw new InvalidOperationException($"Init profile '{profileId}' requires host integration '{group.Host.HostName}', but it is not registered.");
            }
        }

        var project = new
        {
            version = 2,
            groups = configuredGroups.ToDictionary(
                static group => group.Key,
                static group => new
                {
                    kind = group.Value.Kind,
                    sources = group.Value.Sources,
                    excludes = group.Value.Excludes,
                    output = group.Value.Output,
                    host = group.Value.Host is null
                        ? null
                        : new
                        {
                            kind = group.Value.Host.HostName,
                            configuration = group.Value.Host.Configuration,
                        },
                    configuration = group.Value.Configuration,
                },
                StringComparer.OrdinalIgnoreCase),
        };

        File.WriteAllText(fullPath, JsonSerializer.Serialize(project, JsonDefaults.Options));
        Directory.CreateDirectory(Path.Combine(directory, "contracts"));
        Directory.CreateDirectory(Path.Combine(directory, ".merge"));

        foreach (var file in plan.Files)
        {
            var path = Path.Combine(directory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Content);
        }

        var loadedProject = CrimsonProjectFile.Load(fullPath);
        var resolvedGroups = ResolveGroups(loadedProject).ToArray();
        EnsureGitIgnore(
            directory,
            resolvedGroups
                .SelectMany(static group => group.Host is null
                    ? Array.Empty<string>()
                    : group.Host.HostIntegration.GetGitIgnoreEntries(group.Host.Configuration))
                .ToArray());
        PrepareHosts(loadedProject, resolvedGroups);
    }

    public void ValidateProject(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var groups = ResolveGroups(project).ToArray();
        ValidateGroups(groups);
    }

    public void Generate(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var groups = ResolveGroups(project).ToArray();
        ValidateGroups(groups);
        PrepareHosts(project, groups);

        foreach (var group in groups)
        {
            var currentRoot = Path.Combine(project.MergeStateDirectory, group.GroupName, "current");
            RecreateDirectory(currentRoot);

            var descriptors = group.Outputs.ToDictionary(static descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase);
            var outputs = group.Emitter.Emit(group.Model, group.Configuration);

            foreach (var output in outputs)
            {
                if (!descriptors.TryGetValue(output.Name, out var descriptor))
                {
                    throw new InvalidOperationException($"Emitter '{group.Emitter.TargetName}' emitted undeclared output group '{output.Name}' in group '{group.GroupName}'.");
                }

                WriteFiles(Path.Combine(currentRoot, descriptor.RelativeOutputPath), output.Files);
            }
        }
    }

    public MergeResult Merge(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var groups = ResolveGroups(project).ToArray();
        var updated = new List<string>();
        var deleted = new List<string>();
        var conflicts = new List<MergeConflict>();

        foreach (var group in groups)
        {
            var groupStateRoot = Path.Combine(project.MergeStateDirectory, group.GroupName);
            var previousRoot = Path.Combine(groupStateRoot, "previous");
            var currentRoot = Path.Combine(groupStateRoot, "current");
            var backupRoot = Path.Combine(groupStateRoot, "backup");
            var outputRoot = Path.Combine(project.ProjectDirectory, group.OutputRoot);

            Directory.CreateDirectory(previousRoot);
            Directory.CreateDirectory(currentRoot);
            Directory.CreateDirectory(backupRoot);
            Directory.CreateDirectory(outputRoot);

            if (!DirectoryHasFiles(previousRoot))
            {
                _mergeEngine.ReplaceTree(currentRoot, previousRoot);
            }

            RefreshGeneratedBaseline(outputRoot, previousRoot, group.Outputs);

            var result = _mergeEngine.Merge(previousRoot, outputRoot, currentRoot, backupRoot);
            updated.AddRange(result.UpdatedFiles.Select(file => PrefixMergedPath(group, file)));
            deleted.AddRange(result.DeletedFiles.Select(file => PrefixMergedPath(group, file)));
            conflicts.AddRange(result.Conflicts.Select(conflict => new MergeConflict(
                PrefixMergedPath(group, conflict.RelativePath),
                conflict.Reason)));
        }

        if (conflicts.Count > 0)
        {
            return new MergeResult(updated, deleted, conflicts);
        }

        foreach (var group in groups)
        {
            var groupStateRoot = Path.Combine(project.MergeStateDirectory, group.GroupName);
            _mergeEngine.ReplaceTree(
                Path.Combine(groupStateRoot, "current"),
                Path.Combine(groupStateRoot, "previous"));
        }

        return new MergeResult(updated, deleted, conflicts);
    }

    public MergeResult Build(string projectFilePath)
    {
        Generate(projectFilePath);
        return Merge(projectFilePath);
    }

    private static void WriteFiles(string root, IEnumerable<GeneratedFile> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(root, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Content);
        }
    }

    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);
    }

    private static void EnsureGitIgnore(string projectDirectory, IEnumerable<string> additionalEntries)
    {
        var gitIgnorePath = Path.Combine(projectDirectory, ".gitignore");
        var requiredEntries = new[] { ".merge/" }
            .Concat(additionalEntries)
            .Where(static entry => !string.IsNullOrWhiteSpace(entry))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (!File.Exists(gitIgnorePath))
        {
            File.WriteAllText(gitIgnorePath, string.Join(Environment.NewLine, requiredEntries) + Environment.NewLine);
            return;
        }

        var existingLines = new HashSet<string>(File.ReadAllLines(gitIgnorePath), StringComparer.Ordinal);
        var missingEntries = requiredEntries.Where(entry => !existingLines.Contains(entry)).ToArray();
        if (missingEntries.Length == 0)
        {
            return;
        }

        var prefix = File.ReadAllText(gitIgnorePath).EndsWith(Environment.NewLine, StringComparison.Ordinal) ? string.Empty : Environment.NewLine;
        File.AppendAllText(gitIgnorePath, prefix + string.Join(Environment.NewLine, missingEntries) + Environment.NewLine);
    }

    private void ValidateGroups(IReadOnlyList<ConfiguredGroup> groups)
    {
        foreach (var group in groups)
        {
            group.Emitter.ValidateTarget(group.Model, group.Configuration);
            group.Host?.HostIntegration.ValidateHost(group.ProjectFilePath, group.Host.Configuration, group.Host.Group);
        }
    }

    private void PrepareHosts(CrimsonProjectFile project, IReadOnlyList<ConfiguredGroup> groups)
    {
        foreach (var group in groups.Where(static group => group.Host is not null))
        {
            group.Host!.HostIntegration.PrepareProject(project.ProjectFilePath, project.ProjectDirectory, group.Host.Configuration, group.Host.Group);
        }
    }

    private IReadOnlyList<ConfiguredGroup> ResolveGroups(CrimsonProjectFile project)
    {
        return project.Project.Groups
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (!_emitters.TryGetValue(group.Value.Kind, out var emitter))
                {
                    throw new InvalidOperationException($"No target emitter is registered for '{group.Value.Kind}'.");
                }

                var sourceFiles = project.ResolveSourceFiles(group.Value)
                    .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var model = ValidateFiles(sourceFiles);
                var configuration = group.Value.Configuration;
                var outputRoot = ResolveOutputRoot(group.Value, emitter);
                var outputs = emitter.DescribeOutputs(configuration);

                return new ConfiguredGroup(
                    group.Key,
                    project.ProjectFilePath,
                    emitter,
                    configuration,
                    outputRoot,
                    outputs,
                    model,
                    ResolveHostIntegration(project.ProjectFilePath, group.Key, group.Value, emitter, outputRoot, configuration, outputs));
            })
            .ToArray();
    }

    private ConfiguredHostIntegration? ResolveHostIntegration(
        string projectFilePath,
        string groupName,
        CrimsonProjectGroup projectGroup,
        ITargetEmitter emitter,
        string outputRoot,
        JsonElement configuration,
        IReadOnlyList<TargetOutputDescriptor> outputs)
    {
        if (projectGroup.Host is null)
        {
            return null;
        }

        if (!_hostIntegrations.TryGetValue(projectGroup.Host.Kind, out var hostIntegration))
        {
            throw new InvalidOperationException($"No host integration is registered for '{projectGroup.Host.Kind}'.");
        }

        return new ConfiguredHostIntegration(
            hostIntegration,
            projectGroup.Host.Configuration,
            new ResolvedHostGroup(groupName, emitter.TargetName, outputRoot, configuration, outputs));
    }

    private static string ResolveOutputRoot(CrimsonProjectGroup group, ITargetEmitter emitter)
    {
        var output = string.IsNullOrWhiteSpace(group.Output)
            ? emitter.DefaultOutputRoot
            : group.Output!;
        return Utility.PathHelpers.NormalizeRelativePath(output);
    }

    private void RefreshGeneratedBaseline(string outputRoot, string previousRoot, IReadOnlyList<TargetOutputDescriptor> outputs)
    {
        foreach (var relativeRoot in ResolveGeneratedRefreshRoots(outputs))
        {
            _mergeEngine.MirrorLocalTreeAsBase(
                Path.Combine(outputRoot, relativeRoot),
                Path.Combine(previousRoot, relativeRoot));
        }
    }

    private static IReadOnlyList<string> ResolveGeneratedRefreshRoots(IReadOnlyList<TargetOutputDescriptor> outputs) =>
        outputs
            .Where(static output => output.Ownership == TargetOutputOwnership.Generated)
            .Select(static output => NormalizeRefreshRoot(output.RelativeOutputPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeRefreshRoot(string relativeOutputPath)
    {
        var normalized = Utility.PathHelpers.NormalizeRelativePath(relativeOutputPath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var firstSeparator = normalized.IndexOfAny(separators);
        return firstSeparator >= 0 ? normalized[..firstSeparator] : normalized;
    }

    private static bool DirectoryHasFiles(string path) =>
        Directory.Exists(path) && Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any();

    private static string PrefixMergedPath(ConfiguredGroup group, string relativePath)
    {
        var segments = new[] { group.OutputRoot, relativePath }
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
        return Utility.PathHelpers.NormalizeRelativePath(Path.Combine(segments));
    }

    private sealed record ConfiguredGroup(
        string GroupName,
        string ProjectFilePath,
        ITargetEmitter Emitter,
        JsonElement Configuration,
        string OutputRoot,
        IReadOnlyList<TargetOutputDescriptor> Outputs,
        CompilationSetModel Model,
        ConfiguredHostIntegration? Host);

    private sealed record ConfiguredHostIntegration(
        IHostIntegration HostIntegration,
        JsonElement Configuration,
        ResolvedHostGroup Group);
}
