namespace PennMUSH.Core.Communication.Channels;

using System;

[Flags]
public enum ChannelFlags
{
    None = 0,
    Public = 1 << 0,
    Quiet = 1 << 1,
    Admin = 1 << 2,
    Player = 1 << 3,
    Disabled = 1 << 4
}
