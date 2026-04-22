using System.Text.Json;
using Crimson.Core.Generation;
using Crimson.Core.Generation.CSharp;
using Crimson.Core.Generation.Cpp;
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
        var configuredEmitters = (emitters ?? [new CSharpTargetEmitter(), new CppTargetEmitter()])
            .ToArray();
        var configuredHostIntegrations = (hostIntegrations ?? [new DotNetMsbuildHostIntegration(), new CMakeHostIntegration()])
            .ToArray();
        var configuredProfiles = (initProfiles ?? [
            new CSharpProjectInitProfile(),
            new CppCMakeProjectInitProfile(),
            new CppCMakeGccProjectInitProfile(),
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
        var configuredTargets = plan.Targets
            .GroupBy(static target => target.TargetName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                group => group.Count() == 1
                    ? group.Single().Configuration
                    : throw new InvalidOperationException($"Init profile '{profileId}' configured target '{group.Key}' multiple times."),
                StringComparer.OrdinalIgnoreCase);

        foreach (var targetName in configuredTargets.Keys)
        {
            if (!_emitters.ContainsKey(targetName))
            {
                throw new InvalidOperationException($"Init profile '{profileId}' requires target '{targetName}', but no emitter is registered for it.");
            }
        }

        if (plan.Host is not null && !_hostIntegrations.ContainsKey(plan.Host.HostName))
        {
            throw new InvalidOperationException($"Init profile '{profileId}' requires host integration '{plan.Host.HostName}', but it is not registered.");
        }

        var project = new
        {
            sources = plan.Sources,
            excludes = plan.Excludes,
            targets = configuredTargets,
            host = plan.Host is null
                ? null
                : new
                {
                    kind = plan.Host.HostName,
                    configuration = plan.Host.Configuration,
                },
        };

        File.WriteAllText(fullPath, JsonSerializer.Serialize(project, JsonDefaults.Options));
        Directory.CreateDirectory(Path.Combine(directory, "contracts"));
        Directory.CreateDirectory(Path.Combine(directory, ".merge", "previous", "targets"));
        Directory.CreateDirectory(Path.Combine(directory, ".merge", "current", "targets"));
        Directory.CreateDirectory(Path.Combine(directory, ".merge", "backup"));

        foreach (var file in plan.Files)
        {
            var path = Path.Combine(directory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Content);
        }

        var loadedProject = CrimsonProjectFile.Load(fullPath);
        var resolvedTargets = ResolveTargets(loadedProject).ToArray();
        var resolvedHost = ResolveHostIntegration(loadedProject, resolvedTargets);
        EnsureGitIgnore(directory, resolvedHost?.HostIntegration.GetGitIgnoreEntries(resolvedHost.Configuration) ?? Array.Empty<string>());

        if (resolvedHost is not null)
        {
            resolvedHost.HostIntegration.PrepareProject(fullPath, directory, resolvedHost.Configuration, resolvedHost.Targets);
        }
    }

    public void ValidateProject(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var compilation = ValidateFiles(project.ResolveSourceFiles());
        var targets = ResolveTargets(project).ToArray();
        ValidateTargets(project, compilation, targets);
        ValidateHost(project, targets);
    }

    public void Generate(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var model = ValidateFiles(project.ResolveSourceFiles());
        var targets = ResolveTargets(project).ToArray();
        ValidateTargets(project, model, targets);
        ValidateHost(project, targets);

        var currentRoot = Path.Combine(project.MergeStateDirectory, "current");
        RecreateDirectory(currentRoot);

        var resolvedHost = ResolveHostIntegration(project, targets);
        resolvedHost?.HostIntegration.PrepareProject(project.ProjectFilePath, project.ProjectDirectory, resolvedHost.Configuration, resolvedHost.Targets);

        foreach (var target in targets)
        {
            var descriptors = target.Emitter.DescribeOutputs(target.Configuration)
                .ToDictionary(static descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase);
            var outputs = target.Emitter.Emit(model, target.Configuration);
            var targetRoot = Path.Combine(currentRoot, "targets", target.Emitter.TargetName);

            foreach (var output in outputs)
            {
                if (!descriptors.ContainsKey(output.Name))
                {
                    throw new InvalidOperationException($"Emitter '{target.Emitter.TargetName}' emitted undeclared output group '{output.Name}'.");
                }

                WriteFiles(Path.Combine(targetRoot, output.Name), output.Files);
            }
        }
    }

    public MergeResult Merge(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var targets = ResolveTargets(project).ToArray();
        var stateRoot = project.MergeStateDirectory;
        var previousRoot = Path.Combine(stateRoot, "previous");
        var currentRoot = Path.Combine(stateRoot, "current");

        Directory.CreateDirectory(previousRoot);
        Directory.CreateDirectory(currentRoot);
        Directory.CreateDirectory(Path.Combine(stateRoot, "backup"));

        var updated = new List<string>();
        var deleted = new List<string>();
        var conflicts = new List<MergeConflict>();

        foreach (var target in targets)
        {
            var outputRoot = Path.Combine(project.ProjectDirectory, target.Emitter.ResolveOutputRoot(target.Configuration));
            Directory.CreateDirectory(outputRoot);

            foreach (var descriptor in target.Emitter.DescribeOutputs(target.Configuration))
            {
                var stagedPreviousRoot = Path.Combine(previousRoot, "targets", target.Emitter.TargetName, descriptor.Name);
                var stagedCurrentRoot = Path.Combine(currentRoot, "targets", target.Emitter.TargetName, descriptor.Name);
                var projectRoot = Path.Combine(outputRoot, descriptor.RelativeOutputPath);
                var backupRoot = Path.Combine(stateRoot, "backup", "targets", target.Emitter.TargetName, descriptor.Name);

                Directory.CreateDirectory(stagedPreviousRoot);
                Directory.CreateDirectory(stagedCurrentRoot);

                if (descriptor.MergeMode == TargetMergeMode.PreferGenerated)
                {
                    _mergeEngine.MirrorLocalTreeAsBase(projectRoot, stagedPreviousRoot);
                }
                else if (!Directory.EnumerateFiles(stagedPreviousRoot, "*", SearchOption.AllDirectories).Any())
                {
                    _mergeEngine.ReplaceTree(stagedCurrentRoot, stagedPreviousRoot);
                }

                var result = _mergeEngine.Merge(stagedPreviousRoot, projectRoot, stagedCurrentRoot, backupRoot);
                updated.AddRange(result.UpdatedFiles.Select(file => PrefixMergedPath(target, descriptor, file)));
                deleted.AddRange(result.DeletedFiles.Select(file => PrefixMergedPath(target, descriptor, file)));
                conflicts.AddRange(result.Conflicts.Select(conflict => new MergeConflict(
                    PrefixMergedPath(target, descriptor, conflict.RelativePath),
                    conflict.Reason)));
            }
        }

        if (conflicts.Count > 0)
        {
            return new MergeResult(updated, deleted, conflicts);
        }

        _mergeEngine.ReplaceTree(currentRoot, previousRoot);
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

    private static void EnsureGitIgnore(string projectDirectory)
    {
        EnsureGitIgnore(projectDirectory, Array.Empty<string>());
    }

    private static void EnsureGitIgnore(string projectDirectory, IEnumerable<string> additionalEntries)
    {
        var gitIgnorePath = Path.Combine(projectDirectory, ".gitignore");
        var requiredEntries = new[] { ".merge/previous/", ".merge/current/", ".merge/backup/" }
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
    private void ValidateTargets(CrimsonProjectFile project, CompilationSetModel compilation, IReadOnlyList<ConfiguredTarget>? resolvedTargets = null)
    {
        foreach (var target in resolvedTargets ?? ResolveTargets(project).ToArray())
        {
            target.Emitter.ValidateTarget(compilation, target.Configuration);
        }
    }

    private void ValidateHost(CrimsonProjectFile project, IReadOnlyList<ConfiguredTarget>? resolvedTargets = null)
    {
        var host = ResolveHostIntegration(project, resolvedTargets);
        host?.HostIntegration.ValidateHost(project.ProjectFilePath, host.Configuration, host.Targets);
    }

    private IEnumerable<ConfiguredTarget> ResolveTargets(CrimsonProjectFile project)
    {
        foreach (var target in project.Project.Targets.OrderBy(static target => target.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!_emitters.TryGetValue(target.Key, out var emitter))
            {
                throw new InvalidOperationException($"No target emitter is registered for '{target.Key}'.");
            }

            yield return new ConfiguredTarget(
                emitter,
                target.Value,
                emitter.ResolveOutputRoot(target.Value),
                emitter.DescribeOutputs(target.Value));
        }
    }

    private ConfiguredHostIntegration? ResolveHostIntegration(CrimsonProjectFile project, IReadOnlyList<ConfiguredTarget>? resolvedTargets = null)
    {
        if (project.Project.Host is null)
        {
            return null;
        }

        if (!_hostIntegrations.TryGetValue(project.Project.Host.Kind, out var hostIntegration))
        {
            throw new InvalidOperationException($"No host integration is registered for '{project.Project.Host.Kind}'.");
        }

        var targets = resolvedTargets ?? ResolveTargets(project).ToArray();
        return new ConfiguredHostIntegration(
            hostIntegration,
            project.Project.Host.Configuration,
            targets.Select(static target => new ResolvedHostTarget(
                target.Emitter.TargetName,
                target.OutputRoot,
                target.Configuration,
                target.Outputs)).ToArray());
    }

    private static string PrefixMergedPath(ConfiguredTarget target, TargetOutputDescriptor descriptor, string relativePath)
    {
        var segments = new[] { target.OutputRoot, descriptor.RelativeOutputPath, relativePath }
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
        return Utility.PathHelpers.NormalizeRelativePath(Path.Combine(segments));
    }

    private sealed record ConfiguredTarget(
        ITargetEmitter Emitter,
        JsonElement Configuration,
        string OutputRoot,
        IReadOnlyList<TargetOutputDescriptor> Outputs);

    private sealed record ConfiguredHostIntegration(
        IHostIntegration HostIntegration,
        JsonElement Configuration,
        IReadOnlyList<ResolvedHostTarget> Targets);
}
