namespace PennMUSH.Core.Locks;

using System;

[Flags]
public enum LockFlags : uint
{
    None = 0,
    Visual = 0x1,
    Private = 0x2,
    Wizard = 0x4,
    Locked = 0x8,
    NoClone = 0x10,
    Owner = 0x100
}
