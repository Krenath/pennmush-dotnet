namespace PennMUSH.Core.Queue;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Types;

public class QueueEntry
{
    public int Pid { get; init; }
    public Dbref Executor { get; init; }
    public Dbref Enactor { get; init; }
    public string Command { get; init; } = string.Empty;
    public DateTimeOffset ScheduledTime { get; init; } = DateTimeOffset.UtcNow;
    public string? Semaphore { get; init; }
    public string[] Arguments { get; init; } = Array.Empty<string>();
    public Dictionary<string, string> Registers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
