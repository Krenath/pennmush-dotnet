namespace PennMUSH.Core.Locks;

using PennMUSH.Core.Types;

public sealed class LockEntry
{
    public string Type { get; init; } = LockType.Basic;
    public LockNode Key { get; set; } = new ConstantLockNode(true);
    public Dbref Creator { get; set; } = Dbref.Nothing;
    public LockFlags Flags { get; set; } = LockFlags.None;

    public LockEntry() { }

    public LockEntry(string type, LockNode key, Dbref creator, LockFlags flags = LockFlags.None)
    {
        Type = type;
        Key = key;
        Creator = creator;
        Flags = flags;
    }
}
