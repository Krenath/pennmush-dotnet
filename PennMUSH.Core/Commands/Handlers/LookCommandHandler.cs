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
        MUSHObject? target;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            target = execObj != null && execObj.Location.IsValid ? db.GetObject(execObj.Location) : null;
        }
        else
        {
            target = db.MatchObject(context.Executor, context.Arguments);
        }

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

        // If looking at a room or container, show contents
        if (target.Type == ObjectType.Room || target.Type == ObjectType.Thing)
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
        }

        // If looking at a room, show exits
        if (target.Type == ObjectType.Room)
        {
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
}