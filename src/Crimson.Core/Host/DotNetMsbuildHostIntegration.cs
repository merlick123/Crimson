using System.Text.Json;
using Crimson.Core.Generation.CSharp;

namespace Crimson.Core.Host;

public sealed class DotNetMsbuildHostIntegration : IHostIntegration
{
    public string HostName => "dotnet-msbuild";

    public IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration)
    {
        var projectDirectories = ResolveProjectDirectories(configuration);
        return projectDirectories
            .SelectMany(static directory => new[]
            {
                $"{directory}/bin/",
                $"{directory}/obj/",
            })
            .ToArray();
    }

    public void ValidateHost(string projectFilePath, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        _ = HostIntegrationHelpers.RequireTarget(HostName, projectFilePath, targets, "csharp");
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        var csharpTarget = HostIntegrationHelpers.RequireTarget(HostName, projectFilePath, targets, "csharp");

        CSharpBuildIntegration.Write(projectDirectory, new CSharpTargetOptions(csharpTarget.OutputRoot));
    }

    private static IReadOnlyList<string> ResolveProjectDirectories(JsonElement configuration)
    {
        if (configuration.ValueKind != JsonValueKind.Object ||
            !configuration.TryGetProperty("projectDirectories", out var projectDirectoriesElement) ||
            projectDirectoriesElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return projectDirectoriesElement.EnumerateArray()
            .Select(static element => element.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
    }
}
