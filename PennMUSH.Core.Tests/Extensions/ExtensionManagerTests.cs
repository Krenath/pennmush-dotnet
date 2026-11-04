namespace PennMUSH.Core.Tests.Extensions;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using PennMUSH.Extensions;
using PennMUSH.Server.Commands;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Extensions;
using Xunit;

public class ExtensionManagerTests
{
    public class MockExtension : IExtension
    {
        public string Name => "TestPlugin";
        public string Version => "1.0.0";
        public string Description => "Mock Extension for testing";
        public string Author => "TestAuthor";

        public bool Initialized { get; private set; }
        public bool ShutdownCalled { get; private set; }

        public void Initialize(IExtensionHost host)
        {
            Initialized = true;
            host.RegisterCommand(new MockPluginCommand(), "testcmd");
            host.RegisterFunction("plugin_test", (ctx, args) => "PluginFuncOutput");
            host.RegisterProtocolHandler("test.proto", new MockProtocolHandler());
        }

        public void Shutdown()
        {
            ShutdownCalled = true;
        }
    }

    private class MockPluginCommand : ICommandHandler
    {
        public string Name => "testcmd";
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify("Executed MockPluginCommand");
            return true;
        }
    }

    private class MockProtocolHandler : IProtocolHandler
    {
        public bool Handled { get; private set; }
        public Task HandleEnvelopeAsync(Dbref player, string type, JsonElement payload, IExtensionHost host)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExtensionHostProxy_RegistersAndCleansUpHooks()
    {
        var server = new MUSHServer();
        var manager = server.Extensions;
        var hostProxy = new ExtensionHostProxy(manager, server, "TestPlugin");

        var extension = new MockExtension();
        extension.Initialize(hostProxy);

        // 1. Verify command registered
        var lookup = server.Dispatcher.Table.Lookup("testcmd");
        Assert.Equal(CommandLookupStatus.Found, lookup.Status);

        // 2. Verify function registered
        Assert.True(server.Functions.HasFunction("plugin_test"));
        var evalCtx = new EvaluationContext(new Dbref(1), new Dbref(1));
        string res = server.Evaluator.Evaluate("[plugin_test()]", evalCtx);
        Assert.Equal("PluginFuncOutput", res);

        // 3. Verify protocol handler registered
        var jsonDoc = JsonDocument.Parse("{\"foo\":\"bar\"}");
        bool handled = await manager.TryDispatchProtocolEnvelopeAsync(new Dbref(1), "test.proto.action", jsonDoc.RootElement);
        Assert.True(handled);

        // 4. Cleanup and verify unregistration
        hostProxy.Cleanup();
        extension.Shutdown();

        var lookupAfter = server.Dispatcher.Table.Lookup("testcmd");
        Assert.Equal(CommandLookupStatus.NotFound, lookupAfter.Status);
        Assert.False(server.Functions.HasFunction("plugin_test"));
        Assert.True(extension.ShutdownCalled);
    }

    [Fact]
    public void PluginCommandHandler_ListAndPermissions()
    {
        var server = new MUSHServer();
        var handler = new PluginCommandHandler(server.Extensions);
        var db = server.Database;

        var mortal = db.CreateObject("Mortal", ObjectType.Player, Dbref.Nothing);
        var wizard = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wizard.AddFlag("WIZARD");

        // Mortal denied
        var mortalCtx = new CommandContext
        {
            Executor = mortal.Id,
            CommandName = "@plugin"
        };
        handler.Execute(mortalCtx, db);
        Assert.Contains("Permission denied.", mortalCtx.Notifications);

        // Wizard allowed
        var wizCtx = new CommandContext
        {
            Executor = wizard.Id,
            CommandName = "@plugin"
        };
        handler.Execute(wizCtx, db);
        Assert.Contains(wizCtx.Notifications, n => n.Contains("No external plugins are currently loaded.") || n.Contains("Loaded PennMUSH Extensions"));
    }
}
