namespace PennMUSH.Server.Engine;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;
using PennMUSH.Server.Session;

public class MUSHServer
{
    private readonly ConcurrentDictionary<string, MUSHConnection> _connections = new();
    private readonly ConcurrentDictionary<Dbref, string> _playerPasswords = new();
    private readonly CancellationTokenSource _cts = new();

    public ObjectDatabase Database { get; }
    public SqliteStore Storage { get; }
    public ActionQueue Queue { get; }
    public CronScheduler Cron { get; }
    public ChannelRegistry Channels { get; }
    public MailStore Mail { get; }
    public FunctionRegistry Functions { get; }
    public ExpressionEvaluator Evaluator { get; }
    public CommandDispatcher Dispatcher { get; }

    public IReadOnlyCollection<MUSHConnection> Connections => _connections.Values.ToList();

    public MUSHServer(string dbPath = "mush.db")
    {
        Database = new ObjectDatabase();
        Storage = new SqliteStore($"Data Source={dbPath}");
        Queue = new ActionQueue();
        Cron = new CronScheduler();
        Channels = new ChannelRegistry();
        Mail = new MailStore();

        bool loaded = Storage.Load(Database, Cron);
        if (!loaded || Database.Count == 0)
        {
            WorldBootstrapper.GenerateBaseWorld(Database);
            Storage.Save(Database, Cron);
        }

        // Ensure default Public channel
        if (Channels.GetChannel("Public") == null)
        {
            Channels.CreateChannel("Public", new Dbref(1), ChannelFlags.Public | ChannelFlags.Player, "General public discussion channel");
        }

        Functions = new FunctionRegistry(Database);
        Evaluator = new ExpressionEvaluator(Functions.Dispatch);
        Functions.Evaluator = Evaluator;

        AsyncFunctions.Initialize(Queue);
        CronFunctions.Initialize(Cron);

        Dispatcher = new CommandDispatcher(Evaluator);
        RegisterCommands();

        // Start queue tick background task
        _ = Task.Run(() => QueueTickLoopAsync(_cts.Token));
    }

    private void RegisterCommands()
    {
        Dispatcher.Register(new SayCommandHandler(), "say", "\"", "'");
        Dispatcher.Register(new PoseCommandHandler(), "pose", ":", ";");
        Dispatcher.Register(new LookCommandHandler(), "look", "l");
        Dispatcher.Register(new GoCommandHandler(), "go", "goto", "move");
        Dispatcher.Register(new DigCommandHandler(), "@dig");
        Dispatcher.Register(new OpenCommandHandler(), "@open");
        Dispatcher.Register(new LinkCommandHandler(), "@link");
        Dispatcher.Register(new UnlinkCommandHandler(), "@unlink");
        Dispatcher.Register(new CreateCommandHandler(), "@create");
        Dispatcher.Register(new SetCommandHandler(), "@set");
        Dispatcher.Register(new PowerCommandHandler(), "@power");
        Dispatcher.Register(new NameCommandHandler(), "@name");
        Dispatcher.Register(new ParentCommandHandler(), "@parent");
        Dispatcher.Register(new ZoneCommandHandler(), "@zone");
        Dispatcher.Register(new WaitCommandHandler(Queue), "@wait", "@notify");
        Dispatcher.Register(new HaltCommandHandler(Queue), "@halt");
        Dispatcher.Register(new AllHaltCommandHandler(Queue), "@allhalt");
        Dispatcher.Register(new ThinkCommandHandler(Evaluator), "think");
        Dispatcher.Register(new TeleportCommandHandler(), "@teleport", "@tel");
        Dispatcher.Register(new GetCommandHandler(), "get", "take");
        Dispatcher.Register(new DropCommandHandler(), "drop", "throw");
        Dispatcher.Register(new InventoryCommandHandler(), "inventory", "i");
        Dispatcher.Register(new ChannelCommandHandler(Channels), "@channel");
        Dispatcher.Register(new ChatCommandHandler(Channels), "@chat", "+c");
        Dispatcher.Register(new CemitCommandHandler(Channels), "@cemit");
        Dispatcher.Register(new MailCommandHandler(Mail), "@mail");
        Dispatcher.Register(new CronCommandHandler(Cron, Queue), "@cron");
    }

    public void RegisterConnection(MUSHConnection conn)
    {
        _connections[conn.Id] = conn;
    }

    public void UnregisterConnection(MUSHConnection conn)
    {
        _connections.TryRemove(conn.Id, out _);
        if (conn.PlayerId.IsValid)
        {
            var player = Database.GetObject(conn.PlayerId);
            if (player != null && player.Location.IsValid)
            {
                BroadcastToRoom(player.Location, $"{player.Name} has disconnected.", conn.PlayerId);
            }
        }
    }

    public async Task HandleInputAsync(MUSHConnection conn, string input, CancellationToken ct = default)
    {
        string trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        // If not logged in, handle login commands
        if (!conn.PlayerId.IsValid)
        {
            await HandleLoginAsync(conn, trimmed, ct);
            return;
        }

        if (trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
        {
            await conn.SendLineAsync("Goodbye!", ct);
            conn.Close();
            return;
        }

        if (trimmed.Equals("WHO", StringComparison.OrdinalIgnoreCase))
        {
            await ShowWhoAsync(conn, ct);
            return;
        }

        // Dispatch in-game command
        var context = Dispatcher.Dispatch(conn.PlayerId, trimmed, Database);
        foreach (var note in context.Notifications)
        {
            await conn.SendLineAsync(note, ct);
        }

        // Auto-save periodically or after object state changes
        if (context.CommandName.StartsWith('@'))
        {
            Storage.Save(Database, Cron);
        }
    }

    private async Task HandleLoginAsync(MUSHConnection conn, string input, CancellationToken ct)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToUpperInvariant();

        if (cmd == "QUIT")
        {
            await conn.SendLineAsync("Goodbye!", ct);
            conn.Close();
            return;
        }

        if (cmd == "WHO")
        {
            await ShowWhoAsync(conn, ct);
            return;
        }

        if (cmd == "CONNECT" && parts.Length >= 2)
        {
            string name = parts[1];
            string password = parts.Length > 2 ? parts[2] : "";

            var player = Database.GetAllObjects().FirstOrDefault(o =>
                o.Type == ObjectType.Player && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

            if (player != null)
            {
                // Verify password (default "one" for God, or empty for development)
                if (_playerPasswords.TryGetValue(player.Id, out var pass) && pass != password)
                {
                    await conn.SendLineAsync("Incorrect password.", ct);
                    return;
                }

                conn.PlayerId = player.Id;
                await conn.SendLineAsync($"Welcome back to PennMUSH, {player.Name}!", ct);

                // Auto-join Public channel
                var pubCh = Channels.GetChannel("Public");
                pubCh?.AddMember(player.Id);

                // Check unread mail
                var stats = Mail.GetMailStats(player.Id);
                if (stats.UnreadCount > 0)
                {
                    await conn.SendLineAsync($"You have {stats.UnreadCount} unread mail message(s). Type '@mail' to read.", ct);
                }

                // Show room
                Dispatcher.Dispatch(player.Id, "look", Database);
                BroadcastToRoom(player.Location, $"{player.Name} has connected.", player.Id);
                return;
            }

            await conn.SendLineAsync("Either that player does not exist, or has a different password.", ct);
            return;
        }

        if (cmd == "CREATE" && parts.Length >= 3)
        {
            string name = parts[1];
            string password = parts[2];

            bool exists = Database.GetAllObjects().Any(o =>
                o.Type == ObjectType.Player && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                await conn.SendLineAsync("That name is already in use.", ct);
                return;
            }

            var newPlayer = Database.CreateObject(name, ObjectType.Player, Dbref.Nothing);
            newPlayer.Owner = newPlayer.Id;
            newPlayer.Location = new Dbref(0);
            newPlayer.Home = new Dbref(0);
            newPlayer.Pennies = 100;
            _playerPasswords[newPlayer.Id] = password;

            Storage.Save(Database);

            conn.PlayerId = newPlayer.Id;
            await conn.SendLineAsync($"Character {name} created! Welcome to PennMUSH.", ct);

            var pubCh = Channels.GetChannel("Public");
            pubCh?.AddMember(newPlayer.Id);

            Dispatcher.Dispatch(newPlayer.Id, "look", Database);
            BroadcastToRoom(newPlayer.Location, $"{newPlayer.Name} has connected.", newPlayer.Id);
            return;
        }

        await conn.SendLineAsync("Valid login commands: connect <name> <password>, create <name> <password>, WHO, QUIT", ct);
    }

    private async Task ShowWhoAsync(MUSHConnection conn, CancellationToken ct)
    {
        var active = _connections.Values.Where(c => c.PlayerId.IsValid).ToList();
        await conn.SendLineAsync("Player Name          Location   Connected", ct);
        await conn.SendLineAsync("----------------------------------------------------------------", ct);
        foreach (var c in active)
        {
            var p = Database.GetObject(c.PlayerId);
            if (p != null)
            {
                var loc = Database.GetObject(p.Location);
                string locName = loc != null ? loc.Name : p.Location.ToString();
                string duration = FormatDuration(DateTimeOffset.UtcNow - c.ConnectedAt);
                await conn.SendLineAsync($"{p.Name,-20} {locName,-10} {duration}", ct);
            }
        }
        await conn.SendLineAsync($"Total online: {active.Count}", ct);
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    public void BroadcastToRoom(Dbref roomRef, string message, Dbref excludePlayerId = default)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId.IsValid && c.PlayerId != excludePlayerId)
            {
                var p = Database.GetObject(c.PlayerId);
                if (p != null && p.Location == roomRef)
                {
                    _ = c.SendLineAsync(message);
                }
            }
        }
    }

    private async Task QueueTickLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Cron.ProcessDueJobs(DateTimeOffset.UtcNow, Queue);
                Queue.ProcessReady(DateTimeOffset.UtcNow, Dispatcher, Database);
            }
            catch
            {
                // Suppress background tick errors
            }

            await Task.Delay(100, ct);
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        Storage.Save(Database, Cron);
        foreach (var conn in _connections.Values)
        {
            conn.Close();
        }
    }
}
