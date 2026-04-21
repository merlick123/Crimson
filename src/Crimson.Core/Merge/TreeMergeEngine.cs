using Crimson.Core.Utility;

namespace Crimson.Core.Merge;

public sealed record MergeConflict(string RelativePath, string Reason);

public sealed record MergeResult(
    IReadOnlyList<string> UpdatedFiles,
    IReadOnlyList<string> DeletedFiles,
    IReadOnlyList<MergeConflict> Conflicts);

public sealed class TreeMergeEngine
{
    public MergeResult Merge(string baseRoot, string localRoot, string remoteRoot, string backupRoot)
    {
        var relativePaths = EnumerateRelativeFiles(baseRoot)
            .Concat(EnumerateRelativeFiles(localRoot))
            .Concat(EnumerateRelativeFiles(remoteRoot))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var updated = new List<string>();
        var deleted = new List<string>();
        var conflicts = new List<MergeConflict>();
        string? runBackupRoot = null;

        foreach (var relativePath in relativePaths)
        {
            var baseText = ReadFile(baseRoot, relativePath);
            var localText = ReadFile(localRoot, relativePath);
            var remoteText = ReadFile(remoteRoot, relativePath);

            var outcome = MergeFile(baseText, localText, remoteText);
            if (outcome.IsConflict)
            {
                conflicts.Add(new MergeConflict(relativePath, outcome.Reason ?? "Unresolved content conflict."));
                continue;
            }

            var localPath = Path.Combine(localRoot, relativePath);
            if (outcome.Content is null)
            {
                if (File.Exists(localPath))
                {
                    runBackupRoot ??= PrepareBackupRoot(backupRoot);
                    BackupFile(localRoot, localPath, runBackupRoot);
                    File.Delete(localPath);
                    deleted.Add(relativePath);
                }

                continue;
            }

            if (string.Equals(localText, outcome.Content, StringComparison.Ordinal))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            if (File.Exists(localPath))
            {
                runBackupRoot ??= PrepareBackupRoot(backupRoot);
                BackupFile(localRoot, localPath, runBackupRoot);
            }

            File.WriteAllText(localPath, outcome.Content);
            updated.Add(relativePath);
        }

        return new MergeResult(updated, deleted, conflicts);
    }

    public void MirrorGeneratedAsBase(string projectGeneratedRoot, string baseGeneratedRoot)
    {
        if (Directory.Exists(baseGeneratedRoot))
        {
            Directory.Delete(baseGeneratedRoot, recursive: true);
        }

        if (!Directory.Exists(projectGeneratedRoot))
        {
            Directory.CreateDirectory(baseGeneratedRoot);
            return;
        }

        CopyTree(projectGeneratedRoot, baseGeneratedRoot);
    }

    public void ReplaceTree(string sourceRoot, string destinationRoot)
    {
        if (Directory.Exists(destinationRoot))
        {
            Directory.Delete(destinationRoot, recursive: true);
        }

        CopyTree(sourceRoot, destinationRoot);
    }

    private static MergeFileResult MergeFile(string? baseText, string? localText, string? remoteText)
    {
        // If the live project file is missing but both generated baselines agree that
        // the file should exist, heal the project tree back to the expected content.
        if (localText is null &&
            baseText is not null &&
            string.Equals(baseText, remoteText, StringComparison.Ordinal))
        {
            return MergeFileResult.Success(remoteText);
        }

        if (string.Equals(localText, remoteText, StringComparison.Ordinal))
        {
            return MergeFileResult.Success(localText);
        }

        if (string.Equals(localText, baseText, StringComparison.Ordinal))
        {
            return MergeFileResult.Success(remoteText);
        }

        if (string.Equals(remoteText, baseText, StringComparison.Ordinal))
        {
            return MergeFileResult.Success(localText);
        }

        return MergeFileResult.Conflict("Both project and generated state changed differently.");
    }

    private static IEnumerable<string> EnumerateRelativeFiles(string root)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            yield return PathHelpers.NormalizeRelativePath(Path.GetRelativePath(root, file));
        }
    }

    private static string? ReadFile(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static void CopyTree(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);

        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static string PrepareBackupRoot(string backupRoot)
    {
        var runRoot = Path.Combine(backupRoot, PathHelpers.TimestampUtc());
        Directory.CreateDirectory(runRoot);
        return runRoot;
    }

    private static void BackupFile(string localRoot, string localFilePath, string runBackupRoot)
    {
        var relativePath = PathHelpers.NormalizeRelativePath(Path.GetRelativePath(localRoot, localFilePath));
        var backupPath = Path.Combine(runBackupRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(localFilePath, backupPath, overwrite: true);
    }

    private sealed record MergeFileResult(string? Content, bool IsConflict, string? Reason)
    {
        public static MergeFileResult Success(string? content) => new(content, false, null);

        public static MergeFileResult Conflict(string reason) => new(null, true, reason);
    }
}
