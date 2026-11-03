namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;

public class HelpCommandHandler : ICommandHandler
{
    private readonly HelpStore _helpStore;

    public string Name => "help";

    public HelpCommandHandler(HelpStore helpStore)
    {
        _helpStore = helpStore;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string topic = string.IsNullOrWhiteSpace(context.Arguments) ? "help" : context.Arguments.Trim();
        string? result = _helpStore.Lookup(topic);

        if (!string.IsNullOrEmpty(result))
        {
            context.Notify(result);
            return true;
        }

        context.Notify($"No entry for '{topic}'.");
        return false;
    }
}
