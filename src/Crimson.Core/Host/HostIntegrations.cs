using System.Text.Json;
using Crimson.Core.Generation;

namespace Crimson.Core.Host;

public sealed record CrimsonProjectHost(
    string Kind,
    JsonElement Configuration);

public sealed record ResolvedHostGroup(
    string GroupName,
    string TargetKind,
    string OutputRoot,
    JsonElement Configuration,
    IReadOnlyList<TargetOutputDescriptor> Outputs);

public interface IHostIntegration
{
    string HostName { get; }

    IReadOnlyList<string> GetGitIgnoreEntries(JsonElement configuration);

    void ValidateHost(string projectFilePath, JsonElement configuration, ResolvedHostGroup group);

    void PrepareProject(string projectFilePath, string projectDirectory, JsonElement configuration, ResolvedHostGroup group);
}
