using System.Text.Json;
using Crimson.Core.Generation.Rust;

namespace Crimson.Core.Host;

public sealed class CargoHostIntegration : IHostIntegration
{
    public string HostName => "cargo";

    public IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration) =>
        ["target/"];

    public void ValidateHost(string projectFilePath, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        _ = HostIntegrationHelpers.RequireTarget(HostName, projectFilePath, targets, "rust");
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        _ = HostIntegrationHelpers.RequireTarget(HostName, projectFilePath, targets, "rust");

        RustCargoBuildIntegration.Write(projectDirectory, projectFilePath);
    }
}
