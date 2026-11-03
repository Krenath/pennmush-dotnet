namespace PennMUSH.Core.Queue;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public static class PatternMatcher
{
    public static bool Match(string pattern, string input, out string[] captures)
    {
        captures = Array.Empty<string>();
        if (string.IsNullOrEmpty(pattern) || input == null)
            return false;

        // Exact match optimization
        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            if (pattern.Equals(input, StringComparison.OrdinalIgnoreCase))
            {
                captures = new[] { input };
                return true;
            }
            return false;
        }

        // Convert MUSH glob to Regex
        var sb = new StringBuilder("^");
        int groupCount = 0;

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '*')
            {
                sb.Append("(.*?)");
                groupCount++;
            }
            else if (c == '?')
            {
                sb.Append("(.)");
                groupCount++;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }
        sb.Append('$');

        try
        {
            var match = Regex.Match(input, sb.ToString(), RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            var list = new List<string>();
            for (int i = 1; i <= groupCount; i++)
            {
                list.Add(match.Groups[i].Value);
            }
            captures = list.ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
