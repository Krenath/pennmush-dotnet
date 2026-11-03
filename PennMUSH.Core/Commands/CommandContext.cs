namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Types;

public class CommandContext
{
    public Dbref Executor { get; init; }
    public Dbref Enactor { get; init; }
    public string RawCommand { get; init; } = string.Empty;
    public string CommandName { get; init; } = string.Empty;
    public HashSet<string> Switches { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Arguments { get; init; } = string.Empty;
    public string LeftArg { get; init; } = string.Empty;
    public string RightArg { get; init; } = string.Empty;
    public List<string> Notifications { get; } = new();

    public void Notify(string message) => Notifications.Add(message);
}
