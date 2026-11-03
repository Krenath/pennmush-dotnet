namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class HaltCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "@halt";

    public HaltCommandHandler(ActionQueue queue)
    {
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        Dbref target = context.Executor;
        if (!string.IsNullOrWhiteSpace(context.Arguments))
        {
            string s = context.Arguments.Trim();
            if (s.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var execObj = db.GetObject(context.Executor);
                if (execObj == null || !execObj.IsWizard)
                {
                    context.Notify("Permission denied.");
                    return false;
                }
                int allHalted = _queue.AllHalt();
                context.Notify($"Halted all {allHalted} queue entries.");
                return true;
            }

            if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id))
                target = new Dbref(id);
            else if (int.TryParse(s, out int plainId))
                target = new Dbref(plainId);
        }

        var targetObj = db.GetObject(target);
        var executorObj = db.GetObject(context.Executor);
        if (targetObj == null || executorObj == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        if (!executorObj.IsWizard && executorObj.Id != targetObj.Id && executorObj.Id != targetObj.Owner)
        {
            context.Notify("Permission denied.");
            return false;
        }

        int count = _queue.Halt(target);
        context.Notify($"Halted {count} process(es).");
        return true;
    }
}
