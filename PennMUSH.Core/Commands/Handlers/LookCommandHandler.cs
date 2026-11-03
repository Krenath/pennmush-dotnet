namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Types;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;

public class LookCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "look";

    public LookCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        Dbref targetRef;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            targetRef = execObj != null ? execObj.Location : Dbref.Nothing;
        }
        else
        {
            targetRef = ResolveDbref(context.Arguments, context.Executor);
        }

        var target = db.GetObject(targetRef);
        if (target == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        context.Notify(target.Name);

        var desc = target.GetAttribute("DESCRIBE");
        if (desc != null && !string.IsNullOrEmpty(desc.Value))
        {
            if (_evaluator != null)
            {
                var evalCtx = new EvaluationContext(context.Executor, context.Enactor, location: target.Location);
                context.Notify(_evaluator.Evaluate(desc.Value, evalCtx));
            }
            else
            {
                context.Notify(desc.Value);
            }
        }
        else
        {
            context.Notify("You see nothing special.");
        }

        // If looking at a room, show contents and exits
        if (target.Type == ObjectType.Room)
        {
            var contents = new List<string>();
            foreach (var item in db.GetContents(target.Id))
            {
                if (item.Id != context.Executor && !item.HasFlag("DARK"))
                {
                    contents.Add(item.Name);
                }
            }

            if (contents.Count > 0)
            {
                context.Notify("Contents:");
                foreach (var name in contents)
                    context.Notify($"  {name}");
            }

            var exits = new List<string>();
            foreach (var exit in db.GetExits(target.Id))
            {
                if (!exit.HasFlag("DARK"))
                {
                    exits.Add(exit.Name);
                }
            }

            if (exits.Count > 0)
            {
                context.Notify("Obvious exits:");
                foreach (var name in exits)
                    context.Notify($"  {name}");
            }
        }

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