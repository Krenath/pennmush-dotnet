namespace PennMUSH.Core.Locks;

using System;
using PennMUSH.Core.Types;

public static class LockParser
{
    public static LockNode Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ConstantLockNode(true);

        int pos = 0;
        return ParseOr(input, ref pos);
    }

    private static void SkipWhitespace(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos]))
            pos++;
    }

    private static LockNode ParseOr(string s, ref int pos)
    {
        var left = ParseAnd(s, ref pos);
        SkipWhitespace(s, ref pos);

        while (pos < s.Length && s[pos] == '|')
        {
            pos++; // consume '|'
            var right = ParseAnd(s, ref pos);
            left = new OrLockNode(left, right);
            SkipWhitespace(s, ref pos);
        }

        return left;
    }

    private static LockNode ParseAnd(string s, ref int pos)
    {
        var left = ParseUnary(s, ref pos);
        SkipWhitespace(s, ref pos);

        while (pos < s.Length && s[pos] == '&')
        {
            pos++; // consume '&'
            var right = ParseUnary(s, ref pos);
            left = new AndLockNode(left, right);
            SkipWhitespace(s, ref pos);
        }

        return left;
    }

    private static LockNode ParseUnary(string s, ref int pos)
    {
        SkipWhitespace(s, ref pos);
        if (pos >= s.Length)
            return new ConstantLockNode(true);

        if (s[pos] == '!')
        {
            pos++; // consume '!
            var child = ParseUnary(s, ref pos);
            return new NotLockNode(child);
        }

        return ParsePrimary(s, ref pos);
    }

    private static LockNode ParsePrimary(string s, ref int pos)
    {
        SkipWhitespace(s, ref pos);
        if (pos >= s.Length)
            return new ConstantLockNode(true);

        if (s[pos] == '(')
        {
            pos++; // consume '('
            var inner = ParseOr(s, ref pos);
            SkipWhitespace(s, ref pos);
            if (pos < s.Length && s[pos] == ')')
                pos++; // consume ')'
            return inner;
        }

        if (s[pos] == '#')
        {
            int start = pos;
            pos++; // consume '#'
            while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '-'))
                pos++;

            string token = s.Substring(start, pos - start);
            if (Dbref.TryParse(token, out var d))
                return new DbrefLockNode(d);

            return new ConstantLockNode(false);
        }

        // Default or unparsed identifier
        while (pos < s.Length && !char.IsWhiteSpace(s[pos]) && s[pos] != '&' && s[pos] != '|' && s[pos] != ')')
            pos++;

        return new ConstantLockNode(true);
    }
}
