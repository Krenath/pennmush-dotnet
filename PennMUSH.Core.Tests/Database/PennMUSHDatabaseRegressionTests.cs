namespace PennMUSH.Core.Tests.Database;

using System;
using System.IO;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class PennMUSHDatabaseRegressionTests
{
    private static string GetTestDataPath(string filename)
    {
        // Try output directory first, then fallback to source directory
        string localPath = Path.Combine(AppContext.BaseDirectory, "TestData", filename);
        if (File.Exists(localPath)) return localPath;

        string sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", filename);
        if (File.Exists(sourcePath)) return Path.GetFullPath(sourcePath);

        throw new FileNotFoundException($"Cannot locate test file {filename} at {localPath} or {sourcePath}");
    }

    [Fact]
    public void LoadPennMUSHBaseDump_SucceedsWithExactObjectsAndAttributes()
    {
        string path = GetTestDataPath("pennmush_base.dump");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(3, db.Count);

        // Object #0: Room Zero
        var roomZero = db.GetObject(new Dbref(0));
        Assert.NotNull(roomZero);
        Assert.Equal("Room Zero", roomZero.Name);
        Assert.Equal(ObjectType.Room, roomZero.Type);
        Assert.Equal(new Dbref(1), roomZero.Owner);
        Assert.Equal(new Dbref(1), roomZero.Contents);
        Assert.Contains("ACCESSED", roomZero.Flags);
        Assert.Equal("You are in Room Zero.  It's very dark here.", roomZero.GetAttribute("DESCRIBE")?.Value);
        Assert.Equal("is briefly visible through the mist.", roomZero.GetAttribute("OSUCCESS")?.Value);

        // Object #1: One (God / Wizard player)
        var one = db.GetObject(new Dbref(1));
        Assert.NotNull(one);
        Assert.Equal("One", one.Name);
        Assert.Equal(ObjectType.Player, one.Type);
        Assert.Equal(new Dbref(1), one.Owner);
        Assert.Equal(new Dbref(0), one.Location);
        Assert.Contains("WIZARD", one.Flags);
        Assert.Contains("CONNECT", one.Flags);
        Assert.Equal("You see Number One.", one.GetAttribute("DESCRIBE")?.Value);
        Assert.Equal("Sun Jan 17 18:50:49 1993", one.GetAttribute("LAST")?.Value);
        Assert.Equal("disco.seas.upenn.edu", one.GetAttribute("LASTSITE")?.Value);

        // Object #2: Master Room
        var masterRoom = db.GetObject(new Dbref(2));
        Assert.NotNull(masterRoom);
        Assert.Equal("Master Room", masterRoom.Name);
        Assert.Equal(ObjectType.Room, masterRoom.Type);
        Assert.Equal(new Dbref(1), masterRoom.Owner);
        Assert.Contains("FLOATING", masterRoom.Flags);
        Assert.StartsWith("This is the master room", masterRoom.GetAttribute("DESCRIBE")?.Value);
    }

    [Fact]
    public void LoadPennMUSH214Dump_SucceedsWithFullWorldAndSoftcode()
    {
        string path = GetTestDataPath("pennmush_214.dump");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(214, db.Count);

        // #0: Room Zero
        var r0 = db.GetObject(new Dbref(0));
        Assert.NotNull(r0);
        Assert.Equal("Room Zero", r0.Name);
        Assert.Equal(ObjectType.Room, r0.Type);
        Assert.Equal(new Dbref(5), r0.Contents);

        // #1: One (Wizard)
        var one = db.GetObject(new Dbref(1));
        Assert.NotNull(one);
        Assert.Equal("One", one.Name);
        Assert.Equal(ObjectType.Player, one.Type);
        Assert.Contains("WIZARD", one.Flags);
        Assert.Contains("ANSI", one.Flags);
        Assert.Contains("COLOR", one.Flags);
        Assert.Equal("127.0.0.1", one.GetAttribute("LASTIP")?.Value);
        Assert.Equal("AMZN219417B.ant.amazon.com", one.GetAttribute("LASTSITE")?.Value);

        // #2: Master Room
        var mRoom = db.GetObject(new Dbref(2));
        Assert.NotNull(mRoom);
        Assert.Equal("Master Room", mRoom.Name);
        Assert.Contains("FLOATING", mRoom.Flags);

        // #3: Wizard3
        var wiz3 = db.GetObject(new Dbref(3));
        Assert.NotNull(wiz3);
        Assert.Equal("Wizard3", wiz3.Name);
        Assert.Equal(ObjectType.Player, wiz3.Type);
        Assert.Equal(150, wiz3.Pennies);
        Assert.Contains("WIZARD", wiz3.Flags);
        Assert.Contains("SAFE", wiz3.Flags);
        Assert.Equal("Y", wiz3.GetAttribute("ALIAS")?.Value);
        Assert.NotNull(wiz3.GetLock("Use"));
        Assert.NotNull(wiz3.GetLock("Enter"));
        Assert.NotNull(wiz3.GetLock("Basic"));

        // #29: JHV Mordak (Thing with eval lock on Zone)
        var obj29 = db.GetObject(new Dbref(29));
        Assert.NotNull(obj29);
        Assert.Equal("JHV Mordak", obj29.Name);
        Assert.Equal(ObjectType.Thing, obj29.Type);
        Assert.NotNull(obj29.GetLock("Zone"));

        // #212 was garbage in PennMUSH dump and automatically filled
        var obj212 = db.GetObject(new Dbref(212));
        Assert.NotNull(obj212);
        Assert.Equal(ObjectType.Garbage, obj212.Type);

        // #213: Last object in dump
        var obj213 = db.GetObject(new Dbref(213));
        Assert.NotNull(obj213);

        // Verify softcode attribute $WIPESHIP
        var shipWithWipe = db.GetAllObjects().FirstOrDefault(o => o.GetAttribute("WIPE_SHIP") != null);
        Assert.NotNull(shipWithWipe);
        Assert.StartsWith("$WIPESHIP:", shipWithWipe.GetAttribute("WIPE_SHIP")?.Value);
    }

    [Fact]
    public void FlatfileStore_SaveAndReloadPennMUSH214_RoundtripsSuccessfully()
    {
        string path = GetTestDataPath("pennmush_214.dump");
        var originalDb = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(originalDb, reader);
        }

        var sw = new StringWriter();
        FlatfileStore.Save(originalDb, sw);
        string dump = sw.ToString();

        var reloadedDb = new ObjectDatabase();
        using (var reader = new StringReader(dump))
        {
            FlatfileStore.Load(reloadedDb, reader);
        }

        Assert.Equal(originalDb.Count, reloadedDb.Count);

        for (int i = 0; i < originalDb.Count; i++)
        {
            var orig = originalDb.GetObject(new Dbref(i));
            var reloaded = reloadedDb.GetObject(new Dbref(i));

            Assert.NotNull(orig);
            Assert.NotNull(reloaded);
            Assert.Equal(orig.Name, reloaded.Name);
            Assert.Equal(orig.Type, reloaded.Type);
            Assert.Equal(orig.Location, reloaded.Location);
            Assert.Equal(orig.Owner, reloaded.Owner);
            Assert.Equal(orig.Attributes.Count, reloaded.Attributes.Count);
        }
    }
}
