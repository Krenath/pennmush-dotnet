namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;

public class CronCommandHandler : ICommandHandler
{
    private readonly CronScheduler _scheduler;
    private readonly ActionQueue _queue;

    public string Name => "@cron";

    public CronCommandHandler(CronScheduler scheduler, ActionQueue queue)
    {
        _scheduler = scheduler;
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        // Permissions: Wizard or Royalty to manage cron jobs
        if (!executor.IsWizard && !executor.IsGod && !executor.HasFlag("ROYALTY"))
        {
            context.Notify("Permission denied.");
            return false;
        }

        // 1. @cron/add <id>=<expr>,<cmd_or_attr>
        if (context.Switches.Contains("add") || context.Switches.Contains("set"))
        {
            string id = context.LeftArg.Trim();
            string right = context.RightArg;

            int comma = right.IndexOf(',');
            if (string.IsNullOrEmpty(id) || comma <= 0)
            {
                context.Notify("Usage: @cron/add <id>=<cron_expression>,<command_or_target/attr>");
                return false;
            }

            string expression = right.Substring(0, comma).Trim();
            string action = right.Substring(comma + 1).Trim();

            Dbref target = executor.Id;
            string commandToRun = action;

            // Check if action is in format <#dbref>/<ATTR>
            int slash = action.IndexOf('/');
            if (slash > 0 && action.StartsWith('#'))
            {
                string targetStr = action.Substring(0, slash);
                string attrName = action.Substring(slash + 1).Trim();
                if (Dbref.TryParse(targetStr, out var parsedRef) && parsedRef.IsValid)
                {
                    target = parsedRef;
                    commandToRun = $"@trigger {targetStr}/{attrName}";
                }
            }

            if (_scheduler.AddOrUpdateJob(id, expression, executor.Id, target, commandToRun, out string? error))
            {
                var job = _scheduler.GetJob(id)!;
                context.Notify($"Cron job '{id}' added. Next run at: {job.NextRun:yyyy-MM-dd HH:mm:ss} UTC.");
                return true;
            }

            context.Notify($"Error creating cron job: {error}");
            return false;
        }

        // 2. @cron/del or @cron/delete
        if (context.Switches.Contains("del") || context.Switches.Contains("delete"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.DeleteJob(id))
            {
                context.Notify($"Cron job '{id}' deleted.");
                return true;
            }
            context.Notify($"Cron job '{id}' not found.");
            return false;
        }

        // 3. @cron/pause
        if (context.Switches.Contains("pause"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.PauseJob(id))
            {
                context.Notify($"Cron job '{id}' paused.");
                return true;
            }
            context.Notify($"Cron job '{id}' not found.");
            return false;
        }

        // 4. @cron/resume
        if (context.Switches.Contains("resume"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.ResumeJob(id))
            {
                var job = _scheduler.GetJob(id)!;
                context.Notify($"Cron job '{id}' resumed. Next run at: {job.NextRun:yyyy-MM-dd HH:mm:ss} UTC.");
                return true;
            }
            context.Notify($"Cron job '{id}' not found.");
            return false;
        }

        // 5. @cron/run (run immediately)
        if (context.Switches.Contains("run"))
        {
            string id = context.Arguments.Trim();
            var job = _scheduler.GetJob(id);
            if (job == null)
            {
                context.Notify($"Cron job '{id}' not found.");
                return false;
            }

            _queue.Enqueue(job.Target, job.Creator, job.Command);
            job.LastRun = DateTimeOffset.UtcNow;
            context.Notify($"Triggered cron job '{id}' immediately.");
            return true;
        }

        // 6. Default or @cron/list: list all jobs
        var jobs = _scheduler.GetAllJobs();
        if (jobs.Count == 0)
        {
            context.Notify("No cron jobs configured.");
            return true;
        }

        context.Notify("Job ID          Schedule        Status  Target Next Run (UTC)");
        context.Notify("----------------------------------------------------------------------");
        foreach (var job in jobs)
        {
            string status = job.IsPaused ? "PAUSED" : "ACTIVE";
            string next = job.IsPaused ? "---" : job.NextRun.ToString("yyyy-MM-dd HH:mm");
            context.Notify($"{job.Id,-15} {job.Expression,-15} {status,-7} {job.Target,-6} {next}");
        }
        context.Notify("----------------------------------------------------------------------");

        return true;
    }
}
