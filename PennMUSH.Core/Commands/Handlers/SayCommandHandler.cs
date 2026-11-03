namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;

public class SayCommandHandler : ICommandHandler
{
    public string Name => "say";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        context.Notify($"You say, \"{context.Arguments}\"");
        return true;
    }
}