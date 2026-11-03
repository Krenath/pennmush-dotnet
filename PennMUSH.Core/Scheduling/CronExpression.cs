namespace PennMUSH.Core.Scheduling;

using System;
using System.Collections.Generic;

public class CronExpression
{
    private readonly HashSet<int> _minutes = new();
    private readonly HashSet<int> _hours = new();
    private readonly HashSet<int> _daysOfMonth = new();
    private readonly HashSet<int> _months = new();
    private readonly HashSet<int> _daysOfWeek = new();

    public string RawExpression { get; }

    private CronExpression(string raw)
    {
        RawExpression = raw;
    }

    public static bool TryParse(string expression, out CronExpression? result, out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "Cron expression cannot be empty.";
            return false;
        }

        string[] parts = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            error = $"Expected 5 cron fields (min hour dom month dow), got {parts.Length}.";
            return false;
        }

        var cron = new CronExpression(expression.Trim());

        if (!ParseField(parts[0], 0, 59, cron._minutes, out error)) return false;
        if (!ParseField(parts[1], 0, 23, cron._hours, out error)) return false;
        if (!ParseField(parts[2], 1, 31, cron._daysOfMonth, out error)) return false;
        if (!ParseField(parts[3], 1, 12, cron._months, out error)) return false;
        if (!ParseField(parts[4], 0, 6, cron._daysOfWeek, out error)) return false;

        result = cron;
        return true;
    }

    public static CronExpression Parse(string expression)
    {
        if (TryParse(expression, out var result, out var error))
        {
            return result!;
        }
        throw new FormatException(error);
    }

    private static bool ParseField(string field, int min, int max, HashSet<int> target, out string? error)
    {
        error = null;
        string[] subparts = field.Split(',');

        foreach (var sub in subparts)
        {
            string part = sub.Trim();

            if (part == "*")
            {
                for (int i = min; i <= max; i++) target.Add(i);
            }
            else if (part.StartsWith("*/"))
            {
                if (!int.TryParse(part.Substring(2), out int step) || step <= 0)
                {
                    error = $"Invalid step value in '{part}'.";
                    return false;
                }
                for (int i = min; i <= max; i += step) target.Add(i);
            }
            else if (part.Contains('-'))
            {
                string[] range = part.Split('-');
                if (range.Length != 2 || !int.TryParse(range[0], out int rStart) || !int.TryParse(range[1], out int rEnd))
                {
                    error = $"Invalid range in '{part}'.";
                    return false;
                }
                if (rStart < min || rEnd > max || rStart > rEnd)
                {
                    error = $"Range {part} out of bounds ({min}-{max}).";
                    return false;
                }
                for (int i = rStart; i <= rEnd; i++) target.Add(i);
            }
            else
            {
                if (!int.TryParse(part, out int val) || val < min || val > max)
                {
                    error = $"Value '{part}' out of bounds ({min}-{max}).";
                    return false;
                }
                target.Add(val);
            }
        }

        return true;
    }

    public DateTimeOffset GetNextOccurrence(DateTimeOffset from)
    {
        // Start from next minute boundary (seconds and millis truncated)
        var current = from.AddMinutes(1);
        current = new DateTimeOffset(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0, current.Offset);

        // Search up to 5 years forward
        var limit = from.AddYears(5);

        while (current < limit)
        {
            if (!_months.Contains(current.Month))
            {
                current = new DateTimeOffset(current.Year, current.Month, 1, 0, 0, 0, current.Offset).AddMonths(1);
                continue;
            }

            if (!_daysOfMonth.Contains(current.Day))
            {
                current = new DateTimeOffset(current.Year, current.Month, current.Day, 0, 0, 0, current.Offset).AddDays(1);
                continue;
            }

            if (!_daysOfWeek.Contains((int)current.DayOfWeek))
            {
                current = new DateTimeOffset(current.Year, current.Month, current.Day, 0, 0, 0, current.Offset).AddDays(1);
                continue;
            }

            if (!_hours.Contains(current.Hour))
            {
                current = new DateTimeOffset(current.Year, current.Month, current.Day, current.Hour, 0, 0, current.Offset).AddHours(1);
                continue;
            }

            if (!_minutes.Contains(current.Minute))
            {
                current = current.AddMinutes(1);
                continue;
            }

            return current;
        }

        return from.AddMinutes(1);
    }
}
