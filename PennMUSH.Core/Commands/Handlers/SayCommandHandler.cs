namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;

public class SayCommandHandler : ICommandHandler
{
    public string Name => "say";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrEmpty(context.Arguments))
        {
            context.Notify("Say what?");
            return false;
        }

        string message = context.Arguments;
        context.Notify($"You say, \"{message}\"");

        var execObj = db.GetObject(context.Executor);
        if (execObj != null && execObj.Location.IsValid)
        {
            string name = execObj.Name;
            context.NotifyExcept(execObj.Location, context.Executor, $"{name} says, \"{message}\"");
        }

        return true;
    }
}
