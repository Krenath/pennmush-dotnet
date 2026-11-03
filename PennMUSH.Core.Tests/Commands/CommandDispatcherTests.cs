namespace PennMUSH.Core.Tests.Commands;

using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class CommandDispatcherTests
{
    private readonly ObjectDatabase _db;
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _player;

    public CommandDispatcherTests()
    {
        _db = new ObjectDatabase();
        _player = _db.CreateObject("Wizard", ObjectType.Player, new Dbref(1)).Id;

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new SayCommandHandler(), "\"");
        _dispatcher.Register(new PoseCommandHandler(), ":", ";");
        _dispatcher.Register(new CreateCommandHandler());
    }

    [Fact]
    public void SayCommand_EmitsQuoteFormattedMessage()
    {
        var ctx = _dispatcher.Dispatch(_player, "say Greetings adventurer!", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("You say, \"Greetings adventurer!\"", ctx.Notifications[0]);
    }

    [Fact]
    public void SayShortcut_PrefixDoubleQuote_DispatchesToSay()
    {
        var ctx = _dispatcher.Dispatch(_player, "\"Quick greeting", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("You say, \"Quick greeting\"", ctx.Notifications[0]);
    }

    [Fact]
    public void PoseCommand_EmitsNameAndAction()
    {
        var ctx = _dispatcher.Dispatch(_player, "pose bows gracefully.", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("Wizard bows gracefully.", ctx.Notifications[0]);
    }

    [Fact]
    public void PoseShortcut_PrefixColon_DispatchesToPose()
    {
        var ctx = _dispatcher.Dispatch(_player, ":nods silently.", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("Wizard nods silently.", ctx.Notifications[0]);
    }

    [Fact]
    public void CreateCommand_CreatesThingInDatabase_AndNotifies()
    {
        int initialCount = _db.Count;
        var ctx = _dispatcher.Dispatch(_player, "@create Golden Chalice=10", _db);

        Assert.Equal(initialCount + 1, _db.Count);
        Assert.Single(ctx.Notifications);
        Assert.StartsWith("Created: Golden Chalice", ctx.Notifications[0]);

        var chalice = _db.GetObject(new Dbref(initialCount));
        Assert.NotNull(chalice);
        Assert.Equal("Golden Chalice", chalice.Name);
        Assert.Equal(ObjectType.Thing, chalice.Type);
        Assert.Equal(_player, chalice.Owner);
    }

    [Fact]
    public void CreateCommand_WithSilentSwitch_SuppressesNotification()
    {
        var ctx = _dispatcher.Dispatch(_player, "@create/silent Hidden Trap", _db);

        Assert.Empty(ctx.Notifications);
        var created = _db.GetObject(new Dbref(_db.Count - 1));
        Assert.NotNull(created);
        Assert.Equal("Hidden Trap", created.Name);
    }

    [Fact]
    public void UnknownCommand_EmitsPennMUSHHuhFallback()
    {
        var ctx = _dispatcher.Dispatch(_player, "frobnitz 123", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("Huh?  (Type \"help\" for help.)", ctx.Notifications[0]);
    }
}
