namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;

public class PoseCommandHandler : ICommandHandler
{
    public string Name => "pose";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        string name = executor != null ? executor.Name : context.Executor.ToString();
        context.Notify($"{name} {context.Arguments}");
        return true;
    }
}