namespace PennMUSH.Core.Locks;

using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public abstract record LockNode
{
    public abstract bool Evaluate(Dbref player, Dbref target, ObjectDatabase db);
}

public sealed record ConstantLockNode(bool Value) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Value;
}

public sealed record DbrefLockNode(Dbref Target) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => player == Target;
}

public sealed record NotLockNode(LockNode Child) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => !Child.Evaluate(player, target, db);
}

public sealed record AndLockNode(LockNode Left, LockNode Right) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Left.Evaluate(player, target, db) && Right.Evaluate(player, target, db);
}

public sealed record OrLockNode(LockNode Left, LockNode Right) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Left.Evaluate(player, target, db) || Right.Evaluate(player, target, db);
}
