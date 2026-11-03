namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;

public class CemitCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;

    public string Name => "@cemit";

    public CemitCommandHandler(ChannelRegistry channels)
    {
        _channels = channels;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        string chName = context.LeftArg.Trim();
        string message = context.RightArg;

        if (string.IsNullOrEmpty(chName) || string.IsNullOrEmpty(message))
        {
            context.Notify("Usage: @cemit <channel>=<message>");
            return false;
        }

        var ch = _channels.GetChannel(chName);
        if (ch == null)
        {
            context.Notify($"Channel <{chName}> not found.");
            return false;
        }

        if (!executor.IsWizard && !executor.IsGod && ch.Owner != executor.Id)
        {
            context.Notify("Permission denied.");
            return false;
        }

        bool noisy = context.Switches.Contains("noisy");
        string emitText = noisy ? $"[{ch.Name}] {executor.Name}: {message}" : $"[{ch.Name}] {message}";

        var result = _channels.Broadcast(ch.Name, executor.Id, executor.Name, message, isEmit: !noisy, isNoisy: noisy);
        if (result != null)
        {
            context.Notify(result.FormattedMessage);
            return true;
        }

        return false;
    }
}
