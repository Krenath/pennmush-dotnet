using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Network;
using PennMUSH.Server.Session;

var builder = WebApplication.CreateBuilder(args);

// Register Core MUSH Server Singleton
builder.Services.AddSingleton<MUSHServer>();

// Register Telnet Listener Background Service (default port 4201)
builder.Services.AddHostedService<TelnetListenerService>(sp =>
{
    var server = sp.GetRequiredService<MUSHServer>();
    var logger = sp.GetRequiredService<ILogger<TelnetListenerService>>();
    int telnetPort = builder.Configuration.GetValue<int>("Telnet:Port", 4201);
    return new TelnetListenerService(server, logger, telnetPort);
});

var app = builder.Build();

// Enable WebSockets & Static Files (for stylesheets & assets)
app.UseWebSockets();
app.UseStaticFiles();

// Health Check Endpoint
app.MapGet("/health", (MUSHServer server) =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        version = "PennMUSH .NET 10 (Modernized)",
        objects = server.Database.Count,
        connectedSessions = server.Connections.Count,
        channels = server.Channels.GetAllChannels().Select(c => c.Name)
    });
});

// WebSocket Gateway Endpoints (/ws and /game for backward compatibility)
var handleWebSocket = async (HttpContext context, MUSHServer server) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var conn = new MUSHConnection(webSocket);
        server.RegisterConnection(conn);

        // Send initial welcome
        await conn.SendLineAsync("<div class=\"system-msg\">Connected to PennMUSH WebSocket Gateway.</div>");
        await conn.SendLineAsync("<div>Use <b>connect &lt;name&gt; &lt;password&gt;</b> or <b>create &lt;name&gt; &lt;password&gt;</b>.</div>");

        var buffer = new byte[4096];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await server.HandleInputAsync(conn, message);
                }
            }
        }
        catch
        {
            // Socket error / disconnect
        }
        finally
        {
            server.UnregisterConnection(conn);
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Expected WebSocket connection.");
    }
};

app.Map("/ws", handleWebSocket);
app.Map("/game", handleWebSocket);

// Optional API endpoint (/api) for legacy dual-socket MUjs configurations
app.Map("/api", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buf = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            if (r.MessageType == WebSocketMessageType.Close) break;
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("MUjs API Endpoint Active.");
    }
});

app.Run();
