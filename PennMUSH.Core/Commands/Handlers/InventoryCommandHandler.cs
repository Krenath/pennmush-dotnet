namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class InventoryCommandHandler : ICommandHandler
{
    public string Name => "inventory";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        var items = new List<string>();
        foreach (var item in db.GetContents(context.Executor))
        {
            items.Add(item.Name);
        }

        if (items.Count == 0)
        {
            context.Notify("You are empty-handed.");
        }
        else
        {
            context.Notify("You are carrying:");
            foreach (var item in items)
            {
                context.Notify($"  {item}");
            }
        }

        return true;
    }
}
