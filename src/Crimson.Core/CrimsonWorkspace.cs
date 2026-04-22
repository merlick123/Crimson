using System.Text.Json;
using Crimson.Core.Generation;
using Crimson.Core.Generation.CSharp;
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

    public CrimsonWorkspace(IEnumerable<ITargetEmitter>? emitters = null)
    {
        var configuredEmitters = (emitters ?? [new CSharpTargetEmitter()])
            .ToArray();

        _emitters = configuredEmitters
            .GroupBy(static emitter => emitter.TargetName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Count() == 1
                    ? group.Single()
                    : throw new InvalidOperationException($"Multiple target emitters were registered for '{group.Key}'."),
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

    public void InitProject(string projectFilePath, bool starter)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Project directory not found.");
        Directory.CreateDirectory(directory);
        var defaultTargets = _emitters.Values
            .Select(emitter => (emitter.TargetName, Options: emitter.GetDefaultProjectOptions()))
            .Where(static entry => entry.Options is not null)
            .ToDictionary(static entry => entry.TargetName, static entry => entry.Options!, StringComparer.OrdinalIgnoreCase);
        var project = new
        {
            sources = new[] { "contracts/**/*.idl" },
            excludes = Array.Empty<string>(),
            targets = defaultTargets,
        };

        File.WriteAllText(fullPath, JsonSerializer.Serialize(project, JsonDefaults.Options));
        Directory.CreateDirectory(Path.Combine(directory, "contracts"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "raw-previous", "targets"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "raw-current", "targets"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "merge-backup"));
        EnsureGitIgnore(directory);

        foreach (var (targetName, options) in defaultTargets)
        {
            _emitters[targetName].PrepareProject(directory, JsonSerializer.SerializeToElement(options, JsonDefaults.Options));
        }

        if (starter)
        {
            File.WriteAllText(Path.Combine(directory, "contracts", "hello.idl"), StarterIdl);
        }
    }

    public void ValidateProject(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var compilation = ValidateFiles(project.ResolveSourceFiles());
        ValidateTargets(project, compilation);
    }

    public void Generate(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var model = ValidateFiles(project.ResolveSourceFiles());
        var targets = ResolveTargets(project).ToArray();
        ValidateTargets(project, model, targets);

        var rawCurrentRoot = Path.Combine(project.CrimsonStateDirectory, "raw-current");
        RecreateDirectory(rawCurrentRoot);

        foreach (var target in targets)
        {
            target.Emitter.PrepareProject(project.ProjectDirectory, target.Configuration);

            var descriptors = target.Emitter.DescribeOutputs(target.Configuration)
                .ToDictionary(static descriptor => descriptor.Name, StringComparer.OrdinalIgnoreCase);
            var outputs = target.Emitter.Emit(model, target.Configuration);
            var targetRoot = Path.Combine(rawCurrentRoot, "targets", target.Emitter.TargetName);

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
        var stateRoot = project.CrimsonStateDirectory;
        var rawPrevious = Path.Combine(stateRoot, "raw-previous");
        var rawCurrent = Path.Combine(stateRoot, "raw-current");

        Directory.CreateDirectory(rawPrevious);
        Directory.CreateDirectory(rawCurrent);
        Directory.CreateDirectory(Path.Combine(stateRoot, "merge-backup"));

        var updated = new List<string>();
        var deleted = new List<string>();
        var conflicts = new List<MergeConflict>();

        foreach (var target in targets)
        {
            var outputRoot = Path.Combine(project.ProjectDirectory, target.Emitter.ResolveOutputRoot(target.Configuration));
            Directory.CreateDirectory(outputRoot);

            foreach (var descriptor in target.Emitter.DescribeOutputs(target.Configuration))
            {
                var previousRoot = Path.Combine(rawPrevious, "targets", target.Emitter.TargetName, descriptor.Name);
                var currentRoot = Path.Combine(rawCurrent, "targets", target.Emitter.TargetName, descriptor.Name);
                var projectRoot = Path.Combine(outputRoot, descriptor.RelativeOutputPath);
                var backupRoot = Path.Combine(stateRoot, "merge-backup", "targets", target.Emitter.TargetName, descriptor.Name);

                MigrateLegacyOutputGroup(Path.Combine(rawPrevious, descriptor.Name), previousRoot);
                Directory.CreateDirectory(previousRoot);
                Directory.CreateDirectory(currentRoot);

                if (descriptor.MergeMode == TargetMergeMode.PreferGenerated)
                {
                    _mergeEngine.MirrorLocalTreeAsBase(projectRoot, previousRoot);
                }

                var result = _mergeEngine.Merge(previousRoot, projectRoot, currentRoot, backupRoot);
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

        _mergeEngine.ReplaceTree(rawCurrent, rawPrevious);
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
        var gitIgnorePath = Path.Combine(projectDirectory, ".gitignore");
        var requiredEntries = new[]
        {
            ".crimson/raw-previous/",
            ".crimson/raw-current/",
            ".crimson/merge-backup/",
        };

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

    private const string StarterIdl = """
namespace Demo.Contracts {
    /// Example service.
    interface HelloService {
        /// The display name.
        string name;

        /// Greets a user.
        /// @param user_name The user name.
        /// @return The greeting text.
        string greet(string user_name);
    }
}
""";

    private void ValidateTargets(CrimsonProjectFile project, CompilationSetModel compilation, IReadOnlyList<ConfiguredTarget>? resolvedTargets = null)
    {
        foreach (var target in resolvedTargets ?? ResolveTargets(project).ToArray())
        {
            target.Emitter.ValidateTarget(compilation, target.Configuration);
        }
    }

    private IEnumerable<ConfiguredTarget> ResolveTargets(CrimsonProjectFile project)
    {
        foreach (var target in project.Project.Targets.OrderBy(static target => target.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!_emitters.TryGetValue(target.Key, out var emitter))
            {
                throw new InvalidOperationException($"No target emitter is registered for '{target.Key}'.");
            }

            yield return new ConfiguredTarget(emitter, target.Value);
        }
    }

    private static string PrefixMergedPath(ConfiguredTarget target, TargetOutputDescriptor descriptor, string relativePath)
    {
        var segments = new[] { target.Emitter.ResolveOutputRoot(target.Configuration), descriptor.RelativeOutputPath, relativePath }
            .Where(static segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
        return Utility.PathHelpers.NormalizeRelativePath(Path.Combine(segments));
    }

    private static void MigrateLegacyOutputGroup(string legacyRoot, string targetRoot)
    {
        if (!Directory.Exists(legacyRoot))
        {
            return;
        }

        if (Directory.Exists(targetRoot) &&
            Directory.EnumerateFiles(targetRoot, "*", SearchOption.AllDirectories).Any())
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(legacyRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(targetRoot, Path.GetRelativePath(legacyRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(targetRoot, Path.GetRelativePath(legacyRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private sealed record ConfiguredTarget(ITargetEmitter Emitter, JsonElement Configuration);
}
