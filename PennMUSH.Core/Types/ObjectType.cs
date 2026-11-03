using System;

namespace PennMUSH.Core.Types
{
    [Flags]
    public enum ObjectType : uint
    {
        None = 0, Room = 0x1, Thing = 0x2, Exit = 0x4, Player = 0x8, Garbage = 0x10, Marked = 0x20
    }
}