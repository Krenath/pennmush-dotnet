namespace PennMUSH.Server.Session;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;

public class MUSHConnection
{
    private readonly TcpClient? _tcpClient;
    private readonly WebSocket? _webSocket;
    private readonly Func<string, Task>? _onSendLine;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public ClientCapabilities Capabilities { get; } = new();
    public TelnetNegotiator Negotiator { get; }
    public Dbref PlayerId { get; set; } = Dbref.Nothing;
    public ClientProtocolMode ProtocolMode { get; set; } = ClientProtocolMode.StandardTelnet;
    public string? SessionToken { get; set; }
    public HashSet<string> ClientCapabilities { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected => _onSendLine != null || (_tcpClient != null && _tcpClient.Connected) || (_webSocket != null && _webSocket.State == WebSocketState.Open);
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;

    public MUSHConnection(Func<string, Task>? onSendLine)
    {
        _onSendLine = onSendLine;
        Negotiator = new TelnetNegotiator(Capabilities);
    }

    public MUSHConnection(TcpClient tcpClient)
    {
        _tcpClient = tcpClient;
        Negotiator = new TelnetNegotiator(Capabilities);
    }

    public MUSHConnection(WebSocket webSocket)
    {
        _webSocket = webSocket;
        Capabilities.SupportsHtml = true;
        Capabilities.SupportsPueblo = true;
        Capabilities.ColorMode = ColorDepth.TrueColor;
        Negotiator = new TelnetNegotiator(Capabilities);
    }

    public async Task SendEnvelopeAsync<T>(string type, T payload, string? id = null, CancellationToken ct = default)
    {
        string json = ProtocolSerializer.Serialize(type, payload, id);
        if (_onSendLine != null)
        {
            await _onSendLine(json);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await SendRawAsync(bytes, ct);
    }

    public async Task SendLineAsync(string text, CancellationToken ct = default)
    {
        await SendRawAsync(text + "\r\n", ct);
    }

    public async Task SendRawAsync(string text, CancellationToken ct = default)
    {
        if (ProtocolMode == ClientProtocolMode.MujsWebSocket)
        {
            var output = new TerminalOutputPayload(
                Stream: "main",
                Html: text,
                Plain: text.Replace("\r", "").Replace("\n", "")
            );
            await SendEnvelopeAsync("terminal.output", output, null, ct);
            return;
        }

        string formatted = OutputFormatter.FormatOutput(text, Capabilities);
        if (_onSendLine != null)
        {
            await _onSendLine(formatted);
            return;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(formatted);
        await SendRawAsync(bytes, ct);
    }

    public async Task SendRawAsync(byte[] bytes, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct);
        try
        {
            if (_tcpClient != null && _tcpClient.Connected)
            {
                var stream = _tcpClient.GetStream();
                await stream.WriteAsync(bytes, ct);
                await stream.FlushAsync(ct);
            }
            else if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            }
        }
        catch
        {
            // Socket disconnected
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public void Close()
    {
        try { _tcpClient?.Close(); } catch { }
        try { _webSocket?.Dispose(); } catch { }
    }
}
