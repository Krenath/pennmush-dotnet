namespace PennMUSH.Core.Tests.Database;

using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class ObjectDatabaseTests
{
    [Fact]
    public void CreateObject_AssignsSequentialDbrefs()
    {
        var db = new ObjectDatabase();
        Assert.Equal(0, db.Count);
        Assert.Equal(new Dbref(0), db.Top);

        var obj0 = db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        var obj1 = db.CreateObject("Thing One", ObjectType.Thing, new Dbref(1));

        Assert.Equal(new Dbref(0), obj0.Id);
        Assert.Equal(new Dbref(1), obj1.Id);
        Assert.Equal(2, db.Count);
        Assert.Equal(new Dbref(2), db.Top);
    }

    [Fact]
    public void GetObject_RetrievesObject_OrNullIfInvalid()
    {
        var db = new ObjectDatabase();
        var created = db.CreateObject("Test Room", ObjectType.Room, new Dbref(1));

        var retrieved = db.GetObject(created.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Room", retrieved.Name);

        Assert.Null(db.GetObject(new Dbref(999)));
        Assert.Null(db.GetObject(Dbref.Nothing));
    }

    [Fact]
    public void DestroyObject_MarksAsGarbage()
    {
        var db = new ObjectDatabase();
        var obj = db.CreateObject("Victim", ObjectType.Thing, new Dbref(1));

        var destroyed = db.DestroyObject(obj.Id);
        Assert.True(destroyed);

        var fetched = db.GetObject(obj.Id);
        Assert.NotNull(fetched);
        Assert.Equal(ObjectType.Garbage, fetched.Type);
        Assert.Equal("Garbage", fetched.Name);
    }
}
