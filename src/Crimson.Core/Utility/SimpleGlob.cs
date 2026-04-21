using System.Text.RegularExpressions;

namespace Crimson.Core.Utility;

internal sealed class SimpleGlob
{
    private readonly Regex _regex;

    public SimpleGlob(string pattern)
    {
        Pattern = PathHelpers.NormalizeRelativePath(pattern);
        _regex = new Regex("^" + ToRegex(Pattern) + "$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    public string Pattern { get; }

    public bool IsMatch(string relativePath) =>
        _regex.IsMatch(PathHelpers.NormalizeRelativePath(relativePath));

    private static string ToRegex(string pattern)
    {
        var builder = new System.Text.StringBuilder();

        for (var index = 0; index < pattern.Length; index++)
        {
            if (index + 2 < pattern.Length &&
                pattern[index] == '*' &&
                pattern[index + 1] == '*' &&
                pattern[index + 2] == '/')
            {
                builder.Append("(?:.*/)?");
                index += 2;
                continue;
            }

            if (index + 1 < pattern.Length &&
                pattern[index] == '*' &&
                pattern[index + 1] == '*')
            {
                builder.Append(".*");
                index += 1;
                continue;
            }

            if (pattern[index] == '*')
            {
                builder.Append("[^/]*");
                continue;
            }

            if (pattern[index] == '?')
            {
                builder.Append(".");
                continue;
            }

            builder.Append(Regex.Escape(pattern[index].ToString()));
        }

        return builder.ToString();
    }
}
