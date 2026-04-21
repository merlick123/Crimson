using System.Text;
using Crimson.Core.Model;

namespace Crimson.Core.Parsing;

internal static class DocumentationComments
{
    public static DocumentationComment? Parse(IEnumerable<string> rawBlocks)
    {
        var cleaned = rawBlocks
            .SelectMany(CleanLines)
            .ToList();

        if (cleaned.Count == 0)
        {
            return null;
        }

        var summary = new StringBuilder();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        string? returns = null;

        foreach (var line in cleaned)
        {
            if (line.StartsWith("@param ", StringComparison.Ordinal))
            {
                var content = line["@param ".Length..].Trim();
                var firstSpace = content.IndexOf(' ');
                if (firstSpace > 0)
                {
                    parameters[content[..firstSpace]] = content[(firstSpace + 1)..].Trim();
                }

                continue;
            }

            if (line.StartsWith("@return ", StringComparison.Ordinal))
            {
                returns = line["@return ".Length..].Trim();
                continue;
            }

            if (summary.Length > 0)
            {
                summary.AppendLine();
            }

            summary.Append(line);
        }

        return new DocumentationComment(summary.ToString().Trim(), parameters, returns, cleaned);
    }

    private static IEnumerable<string> CleanLines(string raw)
    {
        if (raw.StartsWith("///", StringComparison.Ordinal))
        {
            foreach (var line in raw.Split('\n'))
            {
                var cleaned = line.Trim()
                    .TrimStart('/')
                    .Trim();

                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    yield return cleaned;
                }
            }

            yield break;
        }

        var text = raw
            .Replace("/**", string.Empty, StringComparison.Ordinal)
            .Replace("*/", string.Empty, StringComparison.Ordinal);

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('*'))
            {
                trimmed = trimmed[1..].TrimStart();
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                yield return trimmed;
            }
        }
    }
}
