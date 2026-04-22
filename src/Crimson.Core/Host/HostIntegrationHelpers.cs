namespace Crimson.Core.Host;

internal static class HostIntegrationHelpers
{
    public static ResolvedHostGroup RequireTargetKind(
        string hostName,
        string projectFilePath,
        ResolvedHostGroup group,
        string targetName) =>
        string.Equals(group.TargetKind, targetName, StringComparison.OrdinalIgnoreCase)
            ? group
            : throw new InvalidOperationException($"Host integration '{hostName}' requires a '{targetName}' group in '{projectFilePath}', but group '{group.GroupName}' uses emitter '{group.TargetKind}'.");
}
