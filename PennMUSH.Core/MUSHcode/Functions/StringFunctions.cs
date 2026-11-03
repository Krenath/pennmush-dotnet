namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class StringFunctions
{
    private static readonly Random _random = new();

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("CAT", Cat);
        registry.Register("STRCAT", Strcat);
        registry.Register("STRLEN", Strlen);
        registry.Register("MID", Mid);
        registry.Register("TRIM", Trim);
        registry.Register("UCSTR", Ucstr);
        registry.Register("LCSTR", Lcstr);
        registry.Register("REPEAT", Repeat);
        registry.Register("COMP", Comp);
        registry.Register("EDIT", Edit);
        registry.Register("LJUST", LJust);
        registry.Register("RJUST", RJust);
        registry.Register("CENTER", Center);
        registry.Register("BEFORE", Before);
        registry.Register("AFTER", After);
        registry.Register("LPOS", LPos);
        registry.Register("RPOS", RPos);
        registry.Register("CHR", Chr);
        registry.Register("ORD", Ord);
        registry.Register("CAPSTR", Capstr);
        registry.Register("ART", Art);
        registry.Register("SUBJ", Subj);
        registry.Register("POSS", Poss);
        registry.Register("OBJ", Obj);
        registry.Register("SPACE", Space);
        registry.Register("SCRAMBLE", Scramble);
        registry.Register("SPELLNUM", SpellNum);
        registry.Register("ORDNUMBER", OrdNumber);
        registry.Register("ALPHAMAX", AlphaMax);
        registry.Register("ALPHAMIN", AlphaMin);
        registry.Register("COLUMNS", Columns);
        registry.Register("TABLE", Table);
        registry.Register("WRAP", Wrap);
        registry.Register("STRIP", Strip);
        registry.Register("SECURE", Secure);
        registry.Register("ESCAPE", Escape);
        registry.Register("SOUNDEX", Soundex);
    }

    public static string Cat(EvaluationContext ctx, string[] args)
    {
        return string.Join(" ", args);
    }

    public static string Strcat(EvaluationContext ctx, string[] args)
    {
        return string.Concat(args);
    }

    public static string Strlen(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        return args[0].Length.ToString();
    }

    public static string Mid(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string str = args[0];
        if (!int.TryParse(args[1].Trim(), out int start)) return string.Empty;
        if (!int.TryParse(args[2].Trim(), out int length) || length <= 0) return string.Empty;

        if (start < 0) start = 0;
        if (start >= str.Length) return string.Empty;
        if (start + length > str.Length) length = str.Length - start;

        return str.Substring(start, length);
    }

    public static string Trim(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return args[0].Trim();
    }

    public static string Ucstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return args[0].ToUpperInvariant();
    }

    public static string Lcstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return args[0].ToLowerInvariant();
    }

    public static string Repeat(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string str = args[0];
        if (!int.TryParse(args[1].Trim(), out int count) || count <= 0) return string.Empty;
        if (count > 1000) count = 1000;

        var sb = new StringBuilder(str.Length * count);
        for (int i = 0; i < count; i++)
            sb.Append(str);
        return sb.ToString();
    }

    public static string Comp(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        int cmp = string.CompareOrdinal(args[0], args[1]);
        if (cmp < 0) return "-1";
        if (cmp > 0) return "1";
        return "0";
    }

    public static string Edit(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];

        for (int i = 1; i < args.Length - 1; i += 2)
        {
            string search = args[i];
            string replace = args[i + 1];
            if (!string.IsNullOrEmpty(search))
            {
                text = text.Replace(search, replace);
            }
        }
        return text;
    }

    public static string LJust(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        if (!int.TryParse(args[1].Trim(), out int width) || width <= 0) return text;
        char pad = args.Length > 2 && args[2].Length > 0 ? args[2][0] : ' ';

        return text.Length >= width ? text.Substring(0, width) : text.PadRight(width, pad);
    }

    public static string RJust(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        if (!int.TryParse(args[1].Trim(), out int width) || width <= 0) return text;
        char pad = args.Length > 2 && args[2].Length > 0 ? args[2][0] : ' ';

        return text.Length >= width ? text.Substring(0, width) : text.PadLeft(width, pad);
    }

    public static string Center(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        if (!int.TryParse(args[1].Trim(), out int width) || width <= 0) return text;
        if (text.Length >= width) return text.Substring(0, width);

        char pad = args.Length > 2 && args[2].Length > 0 ? args[2][0] : ' ';
        int totalPad = width - text.Length;
        int leftPad = totalPad / 2;
        int rightPad = totalPad - leftPad;

        return new string(pad, leftPad) + text + new string(pad, rightPad);
    }

    public static string Before(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        string search = args[1];

        if (string.IsNullOrEmpty(search)) return text;
        int idx = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? text.Substring(0, idx) : text;
    }

    public static string After(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string text = args[0];
        string search = args[1];

        if (string.IsNullOrEmpty(search)) return string.Empty;
        int idx = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? text.Substring(idx + search.Length) : string.Empty;
    }

    public static string LPos(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string text = args[0];
        string search = args[1];
        if (string.IsNullOrEmpty(search)) return "0";

        int idx = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        return (idx >= 0 ? idx + 1 : 0).ToString();
    }

    public static string RPos(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string text = args[0];
        string search = args[1];
        if (string.IsNullOrEmpty(search)) return "0";

        int idx = text.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
        return (idx >= 0 ? idx + 1 : 0).ToString();
    }

    public static string Chr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int code) || code < 0 || code > 65535)
            return string.Empty;
        return ((char)code).ToString();
    }

    public static string Ord(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return "0";
        return ((int)args[0][0]).ToString();
    }

    public static string Capstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        string text = args[0];
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    public static string Art(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return string.Empty;
        string word = args[0].TrimStart();
        char firstChar = char.ToLowerInvariant(word[0]);

        bool isVowel = "aeiou".Contains(firstChar);
        return (isVowel ? "an " : "a ") + word;
    }

    public static string Subj(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "it";
        string g = args[0].Trim().ToLowerInvariant();
        return g switch
        {
            "male" or "m" => "he",
            "female" or "f" => "she",
            "plural" or "p" => "they",
            _ => "it"
        };
    }

    public static string Poss(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "its";
        string g = args[0].Trim().ToLowerInvariant();
        return g switch
        {
            "male" or "m" => "his",
            "female" or "f" => "her",
            "plural" or "p" => "their",
            _ => "its"
        };
    }

    public static string Obj(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "it";
        string g = args[0].Trim().ToLowerInvariant();
        return g switch
        {
            "male" or "m" => "him",
            "female" or "f" => "her",
            "plural" or "p" => "them",
            _ => "it"
        };
    }

    public static string Space(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int count) || count <= 0) return string.Empty;
        if (count > 2000) count = 2000;
        return new string(' ', count);
    }

    public static string Scramble(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        char[] chars = args[0].ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    public static string SpellNum(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long num)) return "zero";
        if (num == 0) return "zero";
        if (num < 0) return "negative " + SpellNumberPositive(-num);
        return SpellNumberPositive(num);
    }

    private static string SpellNumberPositive(long number)
    {
        if (number == 0) return string.Empty;
        if (number < 20)
        {
            string[] units = ["", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
                              "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"];
            return units[number];
        }
        if (number < 100)
        {
            string[] tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];
            long remainder = number % 10;
            return tens[number / 10] + (remainder > 0 ? "-" + SpellNumberPositive(remainder) : "");
        }
        if (number < 1000)
        {
            long remainder = number % 100;
            return SpellNumberPositive(number / 100) + " hundred" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        if (number < 1_000_000)
        {
            long remainder = number % 1000;
            return SpellNumberPositive(number / 1000) + " thousand" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        if (number < 1_000_000_000)
        {
            long remainder = number % 1_000_000;
            return SpellNumberPositive(number / 1_000_000) + " million" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        return number.ToString();
    }

    public static string OrdNumber(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int num)) return "0th";
        int abs = Math.Abs(num);
        string suffix = (abs % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (abs % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            }
        };
        return $"{num}{suffix}";
    }

    public static string AlphaMax(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string max = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            if (string.Compare(args[i], max, StringComparison.OrdinalIgnoreCase) > 0)
                max = args[i];
        }
        return max;
    }

    public static string AlphaMin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string min = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            if (string.Compare(args[i], min, StringComparison.OrdinalIgnoreCase) < 0)
                min = args[i];
        }
        return min;
    }

    public static string Columns(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var items = ListFunctions.SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int numCols) || numCols <= 0) return args[0];

        int colWidth = 78 / numCols;
        if (args.Length > 2 && int.TryParse(args[2].Trim(), out int customWidth) && customWidth > 0)
        {
            colWidth = customWidth;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i];
            sb.Append(item.Length >= colWidth ? item.Substring(0, colWidth) : item.PadRight(colWidth));
            if ((i + 1) % numCols == 0 && i < items.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    public static string Table(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var items = ListFunctions.SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int fieldWidth) || fieldWidth <= 0) return args[0];

        int lineWidth = 78;
        if (args.Length > 2 && int.TryParse(args[2].Trim(), out int customLineWidth) && customLineWidth > 0)
        {
            lineWidth = customLineWidth;
        }
        char pad = args.Length > 4 && args[4].Length > 0 ? args[4][0] : ' ';

        int colsPerLine = Math.Max(1, lineWidth / fieldWidth);
        var sb = new StringBuilder();

        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i];
            sb.Append(item.Length >= fieldWidth ? item.Substring(0, fieldWidth) : item.PadRight(fieldWidth, pad));
            if ((i + 1) % colsPerLine == 0 && i < items.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    public static string Wrap(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string text = args[0];
        int width = 78;
        if (args.Length > 1 && int.TryParse(args[1].Trim(), out int customWidth) && customWidth > 0) width = customWidth;

        int firstIndent = args.Length > 2 && int.TryParse(args[2].Trim(), out int fi) && fi >= 0 ? fi : 0;
        int restIndent = args.Length > 3 && int.TryParse(args[3].Trim(), out int ri) && ri >= 0 ? ri : 0;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return string.Empty;

        var lines = new List<string>();
        var currentLine = new StringBuilder();
        int currentMax = width - firstIndent;
        currentLine.Append(new string(' ', firstIndent));

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + (currentLine.Length > firstIndent ? 1 : 0) > currentMax)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentMax = width - restIndent;
                currentLine.Append(new string(' ', restIndent));
            }
            if (currentLine.Length > restIndent && (lines.Count > 0 || currentLine.Length > firstIndent))
            {
                currentLine.Append(' ');
            }
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string Strip(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        // Strip ANSI escape sequences: \x1B\[[0-9;]*[a-zA-Z]
        return Regex.Replace(args[0], @"\x1B\[[0-9;]*[a-zA-Z]", "");
    }

    public static string Secure(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in args[0])
        {
            if (c is '%' or '[' or ']' or '{' or '}')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Escape(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in args[0])
        {
            if (c is '%' or '[' or ']' or '{' or '}' or '(' or ')' or ',' or ';')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Soundex(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return "0000";
        string word = Regex.Replace(args[0].ToUpperInvariant(), @"[^A-Z]", "");
        if (string.IsNullOrEmpty(word)) return "0000";

        var sb = new StringBuilder();
        sb.Append(word[0]);

        char prevCode = GetSoundexCode(word[0]);
        for (int i = 1; i < word.Length && sb.Length < 4; i++)
        {
            char code = GetSoundexCode(word[i]);
            if (code != '0' && code != prevCode)
            {
                sb.Append(code);
            }
            prevCode = code;
        }

        while (sb.Length < 4) sb.Append('0');
        return sb.ToString();
    }

    private static char GetSoundexCode(char c)
    {
        return c switch
        {
            'B' or 'F' or 'P' or 'V' => '1',
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
            'D' or 'T' => '3',
            'L' => '4',
            'M' or 'N' => '5',
            'R' => '6',
            _ => '0'
        };
    }
}
