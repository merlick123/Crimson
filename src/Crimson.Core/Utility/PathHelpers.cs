namespace Crimson.Core.Utility;

internal static class PathHelpers
{
    public static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    public static string TimestampUtc() =>
        DateTime.UtcNow.ToString("yyyyMMddHHmmss");
}
