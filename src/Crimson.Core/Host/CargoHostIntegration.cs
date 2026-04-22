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
        var rustTargets = targets
            .Where(static target => string.Equals(target.TargetName, "rust", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (rustTargets.Length == 0)
        {
            throw new InvalidOperationException($"Host integration '{HostName}' requires a 'rust' target in '{projectFilePath}'.");
        }

        if (rustTargets.Length > 1)
        {
            throw new InvalidOperationException($"Host integration '{HostName}' currently supports a single 'rust' target in '{projectFilePath}'.");
        }
    }

    public void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, IReadOnlyList<ResolvedHostTarget> targets)
    {
        _ = targets.Single(static target => string.Equals(target.TargetName, "rust", StringComparison.OrdinalIgnoreCase));

        RustCargoBuildIntegration.Write(projectDirectory, projectFilePath);
    }
}
