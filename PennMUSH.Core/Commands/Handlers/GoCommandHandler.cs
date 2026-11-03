namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class GoCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "go";

    public GoCommandHandler(LookCommandHandler? lookHandler = null, ExpressionEvaluator? evaluator = null)
    {
        _lookHandler = lookHandler;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Where do you want to go?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string exitName = context.Arguments.Trim();
        var exitObj = db.FindExit(execObj.Location, exitName);

        // Fallback: check dbref directly
        if (exitObj == null && exitName.StartsWith('#') && int.TryParse(exitName.Substring(1), out int exitId))
        {
            var cand = db.GetObject(new Dbref(exitId));
            if (cand != null && cand.Type == ObjectType.Exit && cand.Exits == execObj.Location)
                exitObj = cand;
        }

        if (exitObj == null)
        {
            context.Notify("Alas, you cannot go that way.");
            return false;
        }

        // Check destination
        Dbref destRef = exitObj.Location;
        var destObj = db.GetObject(destRef);
        if (destObj == null)
        {
            context.Notify("That exit leads nowhere.");
            return false;
        }

        // Check lock if present
        var lockAttr = exitObj.GetAttribute("LOCK");
        if (lockAttr != null && !string.IsNullOrWhiteSpace(lockAttr.Value))
        {
            var lockNode = LockParser.Parse(lockAttr.Value);
            bool passed = lockNode.Evaluate(context.Executor, exitObj.Id, db);
            if (!passed)
            {
                var lockCtx = new EvaluationContext(context.Executor, context.Enactor, location: execObj.Location);
                var failAttr = exitObj.GetAttribute("FAIL");
                if (failAttr != null && !string.IsNullOrWhiteSpace(failAttr.Value))
                {
                    context.Notify(_evaluator != null ? _evaluator.Evaluate(failAttr.Value, lockCtx) : failAttr.Value);
                }
                else
                {
                    context.Notify("You can't go that way.");
                }
                return false;
            }
        }

        // Traversal success hooks
        var moveCtx = new EvaluationContext(context.Executor, context.Enactor, location: destRef);
        var succAttr = exitObj.GetAttribute("SUCC");
        if (succAttr != null && !string.IsNullOrWhiteSpace(succAttr.Value))
        {
            context.Notify(_evaluator != null ? _evaluator.Evaluate(succAttr.Value, moveCtx) : succAttr.Value);
        }

        // Move the player
        db.MoveObject(context.Executor, destRef);

        var dropAttr = exitObj.GetAttribute("DROP");
        if (dropAttr != null && !string.IsNullOrWhiteSpace(dropAttr.Value))
        {
            context.Notify(_evaluator != null ? _evaluator.Evaluate(dropAttr.Value, moveCtx) : dropAttr.Value);
        }

        // Auto-look in destination
        if (_lookHandler != null)
        {
            var lookCtx = new CommandContext { Executor = context.Executor, Enactor = context.Enactor };
            _lookHandler.Execute(lookCtx, db);
            foreach (var note in lookCtx.Notifications)
                context.Notify(note);
        }
        else
        {
            context.Notify(destObj.Name);
        }

        return true;
    }
}
