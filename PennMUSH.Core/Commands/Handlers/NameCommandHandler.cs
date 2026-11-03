namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Types;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public class NameCommandHandler : ICommandHandler
{
    public string Name => "@name";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("You must specify a target and a new name.");
            return false;
        }

        Dbref targetRef = ResolveDbref(context.LeftArg, context.Executor);
        var target = db.GetObject(targetRef);
        if (target == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        target.Name = context.RightArg.Trim();
        context.Notify("Name set.");
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