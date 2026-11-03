namespace PennMUSH.Core.Tests.Entities;

using Xunit;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class MUSHObjectTests
{
    [Fact]
    public void MUSHObject_Initialization_SetsProperties()
    {
        var id = new Dbref(1);
        var owner = new Dbref(1);
        var obj = new MUSHObject(id, "Master Room", ObjectType.Room, owner);

        Assert.Equal(id, obj.Id);
        Assert.Equal("Master Room", obj.Name);
        Assert.Equal(ObjectType.Room, obj.Type);
        Assert.Equal(owner, obj.Owner);
        Assert.Equal(Dbref.Nothing, obj.Location);
        Assert.Empty(obj.Attributes);
    }

    [Fact]
    public void SetAttribute_And_GetAttribute_WorkCaseInsensitively()
    {
        var obj = new MUSHObject(new Dbref(0), "God", ObjectType.Player, new Dbref(0));

        var result = obj.SetAttribute("desc", "The Wizard", new Dbref(0), AttributeFlags.Visual);
        Assert.Equal(AttributeErrorCode.Okay, result);
        Assert.True(obj.HasAttribute("DESC"));

        var attr = obj.GetAttribute("DESC");
        Assert.NotNull(attr);
        Assert.Equal("The Wizard", attr.Value);
        Assert.True(attr.Flags.HasFlag(AttributeFlags.Visual));
    }

    [Fact]
    public void SafeAttribute_CannotBeOverwrittenOrCleared()
    {
        var obj = new MUSHObject(new Dbref(10), "Test", ObjectType.Thing, new Dbref(1));

        obj.SetAttribute("LOCKED_ATTR", "protected", new Dbref(1), AttributeFlags.Safe);

        var overwrite = obj.SetAttribute("LOCKED_ATTR", "new value", new Dbref(1));
        Assert.Equal(AttributeErrorCode.Safe, overwrite);
        Assert.Equal("protected", obj.GetAttribute("LOCKED_ATTR")?.Value);

        var clear = obj.ClearAttribute("LOCKED_ATTR");
        Assert.Equal(AttributeErrorCode.Safe, clear);
        Assert.True(obj.HasAttribute("LOCKED_ATTR"));
    }

    [Fact]
    public void ClearAttribute_RemovesExistingAttribute()
    {
        var obj = new MUSHObject(new Dbref(10), "Test", ObjectType.Thing, new Dbref(1));
        obj.SetAttribute("TEMP", "value", new Dbref(1));
        Assert.True(obj.HasAttribute("TEMP"));

        var clear = obj.ClearAttribute("TEMP");
        Assert.Equal(AttributeErrorCode.Okay, clear);
        Assert.False(obj.HasAttribute("TEMP"));
    }
}