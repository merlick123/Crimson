using System.Text.Json;
using Crimson.Core.Host;
using Crimson.Core.Utility;

namespace Crimson.Core.Projects;

public sealed record CrimsonProject(
    int Version,
    IReadOnlyDictionary<string, CrimsonProjectGroup> Groups);

public sealed record CrimsonProjectGroup(
    string Kind,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Excludes,
    string? Output,
    CrimsonProjectHost? Host,
    JsonElement Configuration);

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

        if (project.Version != 2)
        {
            throw new InvalidOperationException($"Unsupported project file version '{project.Version}'. Expected version 2.");
        }

        return new CrimsonProjectFile
        {
            ProjectFilePath = fullPath,
            ProjectDirectory = directory,
            Project = project with
            {
                Groups = NormalizeGroups(project.Groups),
            },
        };
    }

    public static string ResolveInitProjectFilePath(string target, string currentDirectory)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Expected a project name or .crimsonproj path.");
        }

        if (target.EndsWith(".crimsonproj", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(target, currentDirectory);
        }

        var projectDirectory = Path.GetFullPath(target, currentDirectory);
        var projectName = Path.GetFileName(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException("Expected a project name or .crimsonproj path.");
        }

        return Path.Combine(projectDirectory, $"{projectName}.crimsonproj");
    }

    public IEnumerable<string> ResolveSourceFiles(CrimsonProjectGroup group)
    {
        var includeGlobs = group.Sources.Select(static x => new SimpleGlob(x)).ToArray();
        var excludeGlobs = group.Excludes.Select(static x => new SimpleGlob(x)).ToArray();

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

    public string MergeStateDirectory => Path.Combine(ProjectDirectory, ".merge");

    private static IReadOnlyDictionary<string, CrimsonProjectGroup> NormalizeGroups(IReadOnlyDictionary<string, CrimsonProjectGroup>? groups)
    {
        return (groups ?? new Dictionary<string, CrimsonProjectGroup>(StringComparer.OrdinalIgnoreCase))
            .ToDictionary(
                static group => group.Key,
                static group => NormalizeGroup(group.Value),
                StringComparer.OrdinalIgnoreCase);
    }

    private static CrimsonProjectGroup NormalizeGroup(CrimsonProjectGroup group) =>
        group with
        {
            Sources = group.Sources ?? Array.Empty<string>(),
            Excludes = group.Excludes ?? Array.Empty<string>(),
            Host = NormalizeHost(group.Host),
            Configuration = NormalizeConfiguration(group.Configuration),
        };

    private static CrimsonProjectHost? NormalizeHost(CrimsonProjectHost? host)
    {
        if (host is null)
        {
            return null;
        }

        return host with
        {
            Configuration = NormalizeConfiguration(host.Configuration),
        };
    }

    private static JsonElement NormalizeConfiguration(JsonElement configuration) =>
        configuration.ValueKind == JsonValueKind.Object
            ? configuration.Clone()
            : JsonDocument.Parse("{}").RootElement.Clone();
}
