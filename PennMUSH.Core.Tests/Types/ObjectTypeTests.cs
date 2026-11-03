using Xunit;
using PennMUSH.Core.Types;

namespace PennMUSH.Core.Tests.Types;

public class ObjectTypeTests
{
    [Fact]
    public void ObjectTypeEnumValues()
    {
        Assert.Equal(0, (int)ObjectType.None);
        Assert.Equal(0x1, (int)ObjectType.Room);
        Assert.Equal(0x2, (int)ObjectType.Thing);
        Assert.Equal(0x4, (int)ObjectType.Exit);
        Assert.Equal(0x8, (int)ObjectType.Player);
        Assert.Equal(0x10, (int)ObjectType.Garbage);
        Assert.Equal(0x20, (int)ObjectType.Marked);
    }

    [Fact]
    public void BitwiseFlagCombinationsAndHasFlag()
    {
        var combined = ObjectType.Player | ObjectType.Room;
        Assert.True(combined.HasFlag(ObjectType.Player));
        Assert.True(combined.HasFlag(ObjectType.Room));
        Assert.False(combined.HasFlag(ObjectType.Thing));
    }
}