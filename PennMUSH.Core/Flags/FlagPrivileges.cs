namespace PennMUSH.Core.Flags;

using System;

[Flags]
public enum FlagPrivileges : uint
{
    None = 0,
    Any = 0x10,
    Inherit = 0x20,
    Owned = 0x40,
    Royalty = 0x80,
    Wizard = 0x100,
    God = 0x200,
    Internal = 0x400,
    Dark = 0x800,
    Mdark = 0x1000,
    Odark = 0x2000,
    Disabled = 0x4000,
    Log = 0x8000,
    Event = 0x10000
}