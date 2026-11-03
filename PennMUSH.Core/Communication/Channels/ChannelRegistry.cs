namespace PennMUSH.Core.Communication.Channels;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Types;

public record ChannelBroadcastResult(IReadOnlyList<Dbref> Recipients, string FormattedMessage);

public class ChannelRegistry
{
    private readonly ConcurrentDictionary<string, Channel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public bool CreateChannel(string name, Dbref owner, ChannelFlags flags = ChannelFlags.Public | ChannelFlags.Player, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var channel = new Channel(name.Trim(), owner, flags) { Description = description };
        return _channels.TryAdd(name.Trim(), channel);
    }

    public bool DeleteChannel(string name)
    {
        return _channels.TryRemove(name.Trim(), out _);
    }

    public Channel? GetChannel(string name)
    {
        return _channels.TryGetValue(name.Trim(), out var ch) ? ch : null;
    }

    public IReadOnlyList<Channel> GetAllChannels()
    {
        return _channels.Values.OrderBy(c => c.Name).ToList();
    }

    public IReadOnlyList<Channel> GetChannelsForPlayer(Dbref player)
    {
        return _channels.Values.Where(c => c.HasMember(player)).OrderBy(c => c.Name).ToList();
    }

    public ChannelBroadcastResult? Broadcast(
        string channelName,
        Dbref senderId,
        string senderName,
        string text,
        bool isNoisy = false,
        bool isPose = false,
        bool isEmit = false)
    {
        var channel = GetChannel(channelName);
        if (channel == null) return null;

        var member = channel.GetMember(senderId);
        string titlePrefix = (member != null && !string.IsNullOrEmpty(member.Title)) ? $"<{member.Title}> " : "";

        string formatted;
        if (isEmit)
        {
            formatted = $"[{channel.Name}] {text}";
        }
        else if (isPose)
        {
            formatted = $"[{channel.Name}] {titlePrefix}{senderName} {text}";
        }
        else
        {
            formatted = $"[{channel.Name}] {titlePrefix}{senderName}: {text}";
        }

        channel.AddMessage(senderName, senderId, formatted);

        var recipients = channel.Members
            .Where(m => !m.IsMuted)
            .Select(m => m.PlayerId)
            .ToList();

        return new ChannelBroadcastResult(recipients, formatted);
    }
}
