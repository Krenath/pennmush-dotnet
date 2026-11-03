namespace PennMUSH.Core.Scheduling;

using System;
using PennMUSH.Core.Types;

public class CronJob
{
    public string Id { get; init; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public CronExpression ParsedExpression { get; set; } = null!;
    public Dbref Creator { get; init; }
    public Dbref Target { get; set; }
    public string Command { get; set; } = string.Empty;
    public DateTimeOffset? LastRun { get; set; }
    public DateTimeOffset NextRun { get; set; }
    public bool IsPaused { get; set; } = false;

    public CronJob() { }

    public CronJob(string id, string expression, CronExpression parsedExpression, Dbref creator, Dbref target, string command, DateTimeOffset nextRun)
    {
        Id = id;
        Expression = expression;
        ParsedExpression = parsedExpression;
        Creator = creator;
        Target = target;
        Command = command;
        NextRun = nextRun;
    }
}
