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

    public void ValidateHost(string projectFilePath, JsonElement configuration, ResolvedHostGroup group)
    {
        _ = HostIntegrationHelpers.RequireTargetKind(HostName, projectFilePath, group, "csharp");
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, ResolvedHostGroup group)
    {
        var csharpTarget = HostIntegrationHelpers.RequireTargetKind(HostName, projectFilePath, group, "csharp");

        CSharpBuildIntegration.Write(projectDirectory, csharpTarget.GroupName, new CSharpTargetOptions(csharpTarget.OutputRoot));
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
