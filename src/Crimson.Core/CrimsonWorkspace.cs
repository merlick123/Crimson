using System.Text.Json;
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
    private readonly CSharpEmitter _csharpEmitter = new();
    private readonly TreeMergeEngine _mergeEngine = new();
    private readonly CrimsonValidator _validator = new();

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
        var project = new
        {
            sources = new[] { "contracts/**/*.idl" },
            excludes = Array.Empty<string>(),
            targets = new
            {
                csharp = new
                {
                    output = "src",
                },
            },
        };

        File.WriteAllText(fullPath, JsonSerializer.Serialize(project, JsonDefaults.Options));
        Directory.CreateDirectory(Path.Combine(directory, "contracts"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "raw-previous", "User"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "raw-previous", "Generated"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "raw-current"));
        Directory.CreateDirectory(Path.Combine(directory, ".crimson", "merge-backup"));
        CSharpBuildIntegration.Write(directory);

        if (starter)
        {
            File.WriteAllText(Path.Combine(directory, "contracts", "hello.idl"), StarterIdl);
        }
    }

    public void ValidateProject(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        ValidateFiles(project.ResolveSourceFiles());
    }

    public void Generate(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var model = ValidateFiles(project.ResolveSourceFiles());
        var target = _csharpEmitter.Emit(model);
        CSharpBuildIntegration.Write(project.ProjectDirectory);

        var rawCurrentRoot = Path.Combine(project.CrimsonStateDirectory, "raw-current");
        var generatedRoot = Path.Combine(rawCurrentRoot, "Generated");
        var userRoot = Path.Combine(rawCurrentRoot, "User");

        RecreateDirectory(rawCurrentRoot);
        WriteFiles(generatedRoot, target.GeneratedFiles);
        WriteFiles(userRoot, target.UserFiles);
    }

    public MergeResult Merge(string projectFilePath)
    {
        var project = CrimsonProjectFile.Load(projectFilePath);
        var options = project.ResolveCSharpOptions();
        var stateRoot = project.CrimsonStateDirectory;
        var rawPrevious = Path.Combine(stateRoot, "raw-previous");
        var rawCurrent = Path.Combine(stateRoot, "raw-current");
        var backupRoot = Path.Combine(stateRoot, "merge-backup");
        var projectRoot = Path.Combine(project.ProjectDirectory, options.OutputRoot);

        Directory.CreateDirectory(rawPrevious);
        Directory.CreateDirectory(rawCurrent);
        Directory.CreateDirectory(projectRoot);

        _mergeEngine.MirrorGeneratedAsBase(
            Path.Combine(projectRoot, "Generated"),
            Path.Combine(rawPrevious, "Generated"));

        var result = _mergeEngine.Merge(rawPrevious, projectRoot, rawCurrent, backupRoot);
        if (result.Conflicts.Count > 0)
        {
            return result;
        }

        _mergeEngine.ReplaceTree(rawCurrent, rawPrevious);
        return result;
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
}
