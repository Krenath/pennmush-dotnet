using Xunit;
using PennMUSH.Core.Types;

namespace PennMUSH.Core.Tests.Types;

public class DbrefTests
{
    [Fact]
    public void DbrefConstants()
    {
        Assert.Equal(-1, Dbref.Nothing.Value);
        Assert.Equal(-2, Dbref.Ambiguous.Value);
        Assert.Equal(-3, Dbref.Home.Value);
        Assert.Equal(-2, Dbref.AnyOwner.Value);
    }

    [Fact]
    public void ValueProperty()
    {
        var dbref = new Dbref(42);
        Assert.Equal(42, dbref.Value);
    }

    [Fact]
    public void Parse()
    {
        Assert.Equal(42, Dbref.Parse("42").Value);
    }

    [Fact]
    public void TryParse()
    {
        Assert.True(Dbref.TryParse("42", out var d));
        Assert.Equal(42, d.Value);
        Assert.False(Dbref.TryParse(null, out _));
        Assert.False(Dbref.TryParse("invalid", out _));
    }
}