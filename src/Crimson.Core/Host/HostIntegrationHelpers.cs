namespace Crimson.Core.Host;

internal static class HostIntegrationHelpers
{
    public static ResolvedHostTarget RequireTarget(
        string hostName,
        string projectFilePath,
        IReadOnlyList<ResolvedHostTarget> targets,
        string targetName) =>
        targets.SingleOrDefault(target => string.Equals(target.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Host integration '{hostName}' requires a '{targetName}' target in '{projectFilePath}'.");
}
