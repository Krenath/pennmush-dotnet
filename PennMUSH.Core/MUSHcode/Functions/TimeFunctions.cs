namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Globalization;

public static class TimeFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("TIME", Time);
        registry.Register("SECS", Secs);
        registry.Register("CONVSECS", ConvSecs);
        registry.Register("CONVTIME", ConvTime);
        registry.Register("SECS2TIME", Secs2Time);
        registry.Register("TIMEFMT", TimeFmt);
        registry.Register("ETIMEFMT", ETimeFmt);
        registry.Register("ISDAYLIGHT", IsDaylight);
        registry.Register("TZOFFSET", TzOffset);
        registry.Register("MTIME", (ctx, args) => MTime(registry, ctx, args));
    }

    public static string Time(EvaluationContext ctx, string[] args)
    {
        return DateTime.Now.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    }

    public static string Secs(EvaluationContext ctx, string[] args)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    public static string ConvSecs(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long secs))
        {
            secs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        var dt = DateTimeOffset.FromUnixTimeSeconds(secs).ToLocalTime();
        return dt.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    }

    public static string ConvTime(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "-1";
        string input = args[0].Trim();
        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToUnixTimeSeconds().ToString();
        }
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return new DateTimeOffset(dt).ToUnixTimeSeconds().ToString();
        }
        return "-1";
    }

    public static string Secs2Time(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long totalSeconds)) return "0s";
        if (totalSeconds <= 0) return "0s";

        long days = totalSeconds / 86400;
        long hours = (totalSeconds % 86400) / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        var parts = new System.Collections.Generic.List<string>();
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (minutes > 0) parts.Add($"{minutes}m");
        if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds}s");

        return string.Join(" ", parts);
    }

    public static string TimeFmt(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string format = args[0];
        long secs = args.Length > 1 && long.TryParse(args[1].Trim(), out long s)
            ? s
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var dt = DateTimeOffset.FromUnixTimeSeconds(secs).ToLocalTime();

        // Convert standard strftime tokens to .NET format
        string netFmt = format
            .Replace("%Y", dt.ToString("yyyy"))
            .Replace("%y", dt.ToString("yy"))
            .Replace("%m", dt.ToString("MM"))
            .Replace("%B", dt.ToString("MMMM"))
            .Replace("%b", dt.ToString("MMM"))
            .Replace("%d", dt.ToString("dd"))
            .Replace("%H", dt.ToString("HH"))
            .Replace("%I", dt.ToString("hh"))
            .Replace("%M", dt.ToString("mm"))
            .Replace("%S", dt.ToString("ss"))
            .Replace("%p", dt.ToString("tt"))
            .Replace("%a", dt.ToString("ddd"))
            .Replace("%A", dt.ToString("dddd"))
            .Replace("%%", "%");

        return netFmt;
    }

    public static string ETimeFmt(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string format = args[0];
        if (!long.TryParse(args[1].Trim(), out long totalSeconds)) return "0";

        long days = totalSeconds / 86400;
        long hours = (totalSeconds % 86400) / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        return format
            .Replace("$d", days.ToString())
            .Replace("$h", hours.ToString("D2"))
            .Replace("$m", minutes.ToString("D2"))
            .Replace("$s", seconds.ToString("D2"));
    }

    public static string IsDaylight(EvaluationContext ctx, string[] args)
    {
        return TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? "1" : "0";
    }

    public static string TzOffset(EvaluationContext ctx, string[] args)
    {
        return ((long)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds).ToString();
    }

    public static string MTime(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return "-1";
        var dbref = DbFunctions.ParseDbref(args[0], ctx);
        if (!dbref.IsValid) return "-1";

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return "-1";

        return obj.ModificationTime.ToUnixTimeSeconds().ToString();
    }
}
