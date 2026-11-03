namespace PennMUSH.Core.Attributes;

using System;

[Flags]
public enum AttributeFlags : uint
{
    None = 0,
    Quiet = 0x1,
    Internal = 0x2,
    Wizard = 0x4,
    Locked = 0x10,
    NoProg = 0x20,
    MDark = 0x40,
    Private = 0x80,
    NoCopy = 0x100,
    Visual = 0x200,
    Regexp = 0x400,
    Case = 0x800,
    Safe = 0x1000,
    Root = 0x2000,
    RLimit = 0x4000,
    Enum = 0x8000,
    Command = 0x20000,
    Listen = 0x40000,
    NoDump = 0x80000,
    PrefixMatch = 0x200000,
    Veiled = 0x400000,
    Debug = 0x800000,
    Nearby = 0x1000000,
    Public = 0x2000000,
    Anon = 0x4000000,
    NoName = 0x8000000,
    NoSpace = 0x10000000,
    MHear = 0x20000000,
    AHear = 0x40000000,
    NoDebug = 0x80000000
}
