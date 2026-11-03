namespace PennMUSH.Core.Tests.Locks;

using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public class LockParserTests
{
    private readonly ObjectDatabase _db = new();
    private readonly Dbref _target = new(10);

    [Fact]
    public void EmptyOrNullLock_EvaluatesToTrue()
    {
        var node1 = LockParser.Parse(null);
        var node2 = LockParser.Parse("   ");

        Assert.True(node1.Evaluate(new Dbref(1), _target, _db));
        Assert.True(node2.Evaluate(new Dbref(1), _target, _db));
    }

    [Fact]
    public void SimpleDbrefLock_EvaluatesCorrectly()
    {
        var lockNode = LockParser.Parse("#1");

        Assert.True(lockNode.Evaluate(new Dbref(1), _target, _db));
        Assert.False(lockNode.Evaluate(new Dbref(2), _target, _db));
    }

    [Fact]
    public void NotLock_InvertsResult()
    {
        var lockNode = LockParser.Parse("!#1");

        Assert.False(lockNode.Evaluate(new Dbref(1), _target, _db));
        Assert.True(lockNode.Evaluate(new Dbref(2), _target, _db));
    }

    [Fact]
    public void OrLock_RequiresEitherKey()
    {
        var lockNode = LockParser.Parse("#1 | #2");

        Assert.True(lockNode.Evaluate(new Dbref(1), _target, _db));
        Assert.True(lockNode.Evaluate(new Dbref(2), _target, _db));
        Assert.False(lockNode.Evaluate(new Dbref(3), _target, _db));
    }

    [Fact]
    public void ComplexLock_WithParentheses_EvaluatesPrecedenceCorrectly()
    {
        // !#1 & (#2 | #3)
        var lockNode = LockParser.Parse("!#1 & (#2 | #3)");

        Assert.False(lockNode.Evaluate(new Dbref(1), _target, _db));
        Assert.True(lockNode.Evaluate(new Dbref(2), _target, _db));
        Assert.True(lockNode.Evaluate(new Dbref(3), _target, _db));
        Assert.False(lockNode.Evaluate(new Dbref(4), _target, _db));
    }
}
