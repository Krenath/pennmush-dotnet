namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;

public class WallCommandHandler : ICommandHandler
{
    public string Name => "@wall";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Broadcast what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !(execObj.IsWizard || execObj.IsRoyalty))
        {
            context.Notify("Permission denied.");
            return false;
        }

        string prefix = context.CommandName.ToLowerInvariant() switch
        {
            "@wizwall" => "[Wizwall]",
            "@rwall" => "[Rwall]",
            _ => "[Broadcast]"
        };

        bool wizOnly = context.CommandName.Equals("@wizwall", StringComparison.OrdinalIgnoreCase) ||
                       context.CommandName.Equals("@rwall", StringComparison.OrdinalIgnoreCase);

        context.NotifyBroadcast($"{prefix} {execObj.Name}: {context.Arguments}", wizardOnly: wizOnly);
        return true;
    }
}
