namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Linq;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;

public static class CronFunctions
{
    private static CronScheduler? _scheduler;

    public static void Initialize(CronScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public static void Register(FunctionRegistry registry, CronScheduler? scheduler = null)
    {
        if (scheduler != null) _scheduler = scheduler;

        registry.Register("CRON_LIST", CronList);
        registry.Register("CRON_INFO", CronInfo);
    }

    public static string CronList(EvaluationContext ctx, string[] args)
    {
        if (_scheduler == null) return string.Empty;
        string pattern = args.Length > 0 ? args[0].Trim() : "*";

        var ids = _scheduler.GetAllJobs()
            .Select(j => j.Id)
            .Where(id => PatternMatcher.Match(pattern, id, out _));

        return string.Join(" ", ids);
    }

    public static string CronInfo(EvaluationContext ctx, string[] args)
    {
        if (_scheduler == null || args.Length == 0) return "#-1 NO MATCH";
        string id = args[0].Trim();
        var job = _scheduler.GetJob(id);
        if (job == null) return "#-1 NO MATCH";

        string field = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "schedule";

        return field switch
        {
            "schedule" or "expr" => job.Expression,
            "command" or "cmd" => job.Command,
            "target" => job.Target.ToString(),
            "creator" => job.Creator.ToString(),
            "nextrun" or "next" => job.NextRun.ToUnixTimeSeconds().ToString(),
            "lastrun" or "last" => job.LastRun.HasValue ? job.LastRun.Value.ToUnixTimeSeconds().ToString() : "-1",
            "paused" => job.IsPaused ? "1" : "0",
            _ => job.Expression
        };
    }
}
