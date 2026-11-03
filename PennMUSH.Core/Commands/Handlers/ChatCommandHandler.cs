namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;

public class ChatCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;

    public string Name => "@chat";

    public ChatCommandHandler(ChannelRegistry channels)
    {
        _channels = channels;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        string chName = context.LeftArg.Trim();
        string message = context.RightArg;

        // If no '=' was provided, try splitting by first space: "+channel message"
        if (string.IsNullOrEmpty(context.RightArg) && !string.IsNullOrEmpty(context.Arguments))
        {
            int space = context.Arguments.IndexOf(' ');
            if (space > 0)
            {
                chName = context.Arguments.Substring(0, space).TrimStart('+');
                message = context.Arguments.Substring(space + 1).TrimStart();
            }
        }

        if (string.IsNullOrEmpty(chName) || string.IsNullOrEmpty(message))
        {
            context.Notify("Usage: @chat <channel>=<message>");
            return false;
        }

        var ch = _channels.GetChannel(chName);
        if (ch == null)
        {
            context.Notify($"Channel <{chName}> not found.");
            return false;
        }

        if (!ch.HasMember(executor.Id) && !executor.IsWizard)
        {
            context.Notify($"You are not on channel <{ch.Name}>.");
            return false;
        }

        bool isPose = false;
        if (message.StartsWith(':'))
        {
            isPose = true;
            message = message.Substring(1);
        }
        else if (message.StartsWith(';'))
        {
            isPose = true;
            message = message.Substring(1);
        }

        var result = _channels.Broadcast(ch.Name, executor.Id, executor.Name, message, isPose: isPose);
        if (result != null)
        {
            context.Notify(result.FormattedMessage);
            return true;
        }

        return false;
    }
}
