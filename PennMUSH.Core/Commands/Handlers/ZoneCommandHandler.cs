namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ZoneCommandHandler : ICommandHandler
{
    public string Name => "@zone";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify("What object do you want to zone?");
            return false;
        }

        Dbref targetRef = ResolveDbref(context.LeftArg, context.Executor);
        var targetObj = db.GetObject(targetRef);
        if (targetObj == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || (!execObj.IsWizard && execObj.Id != targetObj.Owner))
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg) || context.RightArg.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            targetObj.ClearZones();
            context.Notify("Zone cleared.");
            return true;
        }

        var tokens = context.RightArg.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validZones = new List<Dbref>();

        foreach (var token in tokens)
        {
            Dbref zRef = ResolveDbref(token, context.Executor);
            var zObj = db.GetObject(zRef);
            if (zObj == null)
            {
                context.Notify($"I can't see zone object {token}.");
                return false;
            }

            validZones.Add(zRef);
        }

        targetObj.SetZones(validZones);
        context.Notify("Zone set.");
        return true;
    }

    private static Dbref ResolveDbref(string s, Dbref executor)
    {
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor;
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id)) return new Dbref(id);
        if (int.TryParse(s, out int plainId)) return new Dbref(plainId);
        return Dbref.Nothing;
    }
}
