namespace PennMUSH.Core.Communication.Channels;

using PennMUSH.Core.Types;

public class ChannelMember
{
    public Dbref PlayerId { get; init; }
    public string Title { get; set; } = string.Empty;
    public bool IsMuted { get; set; } = false;
    public bool IsHidden { get; set; } = false;

    public ChannelMember(Dbref playerId, string title = "")
    {
        PlayerId = playerId;
        Title = title;
    }
}
