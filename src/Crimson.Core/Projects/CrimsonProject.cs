using System.Text.Json;
using Crimson.Core.Utility;

namespace Crimson.Core.Projects;

public sealed record CrimsonProject(
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Excludes,
    IReadOnlyDictionary<string, JsonElement> Targets);

public sealed record CSharpTargetOptions(string OutputRoot)
{
    public static CSharpTargetOptions Default => new("out/csharp");
}

public sealed class CrimsonProjectFile
{
    public required string ProjectFilePath { get; init; }
    public required string ProjectDirectory { get; init; }
    public required CrimsonProject Project { get; init; }

    public static CrimsonProjectFile Load(string projectFilePath)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("Project directory not found.");
        var json = File.ReadAllText(fullPath);
        var project = JsonSerializer.Deserialize<CrimsonProject>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Project file could not be parsed.");

        return new CrimsonProjectFile
        {
            ProjectFilePath = fullPath,
            ProjectDirectory = directory,
            Project = project with
            {
                Sources = project.Sources ?? Array.Empty<string>(),
                Excludes = project.Excludes ?? Array.Empty<string>(),
                Targets = project.Targets ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase),
            },
        };
    }

    public IEnumerable<string> ResolveSourceFiles()
    {
        var includeGlobs = Project.Sources.Select(static x => new SimpleGlob(x)).ToArray();
        var excludeGlobs = Project.Excludes.Select(static x => new SimpleGlob(x)).ToArray();

        foreach (var file in Directory.EnumerateFiles(ProjectDirectory, "*.idl", SearchOption.AllDirectories))
        {
            var relative = PathHelpers.NormalizeRelativePath(Path.GetRelativePath(ProjectDirectory, file));

            if (includeGlobs.Length > 0 && !includeGlobs.Any(x => x.IsMatch(relative)))
            {
                continue;
            }

            if (excludeGlobs.Any(x => x.IsMatch(relative)))
            {
                continue;
            }

            yield return file;
        }
    }

    public CSharpTargetOptions ResolveCSharpOptions()
    {
        if (!Project.Targets.TryGetValue("csharp", out var element))
        {
            return CSharpTargetOptions.Default;
        }

        var outputRoot = element.TryGetProperty("output", out var outputElement)
            ? outputElement.GetString()
            : null;

        return new CSharpTargetOptions(outputRoot ?? CSharpTargetOptions.Default.OutputRoot);
    }

    public string CrimsonStateDirectory => Path.Combine(ProjectDirectory, ".crimson");
}
