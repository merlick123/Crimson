using System.Text.Json;
using Crimson.Core.Generation.Rust;

namespace Crimson.Core.Host;

public sealed class CargoHostIntegration : IHostIntegration
{
    public string HostName => "cargo";

    public IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration) =>
        ["target/"];

    public void ValidateHost(string projectFilePath, JsonElement configuration, ResolvedHostGroup group)
    {
        _ = HostIntegrationHelpers.RequireTargetKind(HostName, projectFilePath, group, "rust");
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, ResolvedHostGroup group)
    {
        var rustGroup = HostIntegrationHelpers.RequireTargetKind(HostName, projectFilePath, group, "rust");

        RustCargoBuildIntegration.Write(projectDirectory, projectFilePath, rustGroup.GroupName);
    }
}
