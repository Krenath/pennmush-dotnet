namespace PennMUSH.Core.Commands;

using PennMUSH.Core.Database;

public interface ICommandHandler
{
    string Name { get; }
    bool Execute(CommandContext context, ObjectDatabase db);
}