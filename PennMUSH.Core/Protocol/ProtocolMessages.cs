namespace PennMUSH.Core.Protocol;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum ClientProtocolMode
{
    StandardTelnet = 0,
    MujsWebSocket = 1,
    MujsTelnetGMCP = 2
}

public record ProtocolEnvelope<T>
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("payload")]
    public T Payload { get; init; } = default!;
}

public static class ProtocolSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Serialize<T>(string type, T payload, string? id = null)
    {
        var env = new ProtocolEnvelope<T>
        {
            Type = type,
            Id = id,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Payload = payload
        };
        return JsonSerializer.Serialize(env, Options);
    }

    public static string SerializeRaw<T>(ProtocolEnvelope<T> envelope)
    {
        return JsonSerializer.Serialize(envelope, Options);
    }

    public static bool TryParseType(string json, out string? type, out string? id)
    {
        type = null;
        id = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out var typeProp))
            {
                type = typeProp.GetString();
                if (root.TryGetProperty("id", out var idProp))
                {
                    id = idProp.GetString();
                }
                return !string.IsNullOrEmpty(type);
            }
        }
        catch
        {
            // Not a JSON object
        }
        return false;
    }

    public static T? DeserializePayload<T>(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("payload", out var payloadProp))
            {
                return payloadProp.Deserialize<T>(Options);
            }
        }
        catch
        {
            // Deserialization failure
        }
        return default;
    }
}

// Handshake Payloads
public record ClientHelloPayload(
    [property: JsonPropertyName("client")] string Client,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("capabilities")] string[] Capabilities
);

public record ServerHelloPayload(
    [property: JsonPropertyName("server")] string Server,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("sessionToken")] string SessionToken,
    [property: JsonPropertyName("features")] ServerFeatures Features
);

public record ServerFeatures(
    [property: JsonPropertyName("maxWindowCount")] int MaxWindowCount,
    [property: JsonPropertyName("supportedLanguages")] string[] SupportedLanguages,
    [property: JsonPropertyName("assetBaseUrl")] string AssetBaseUrl
);

// Terminal Payloads
public record TerminalInputPayload(
    [property: JsonPropertyName("command")] string Command
);

public record TerminalOutputPayload(
    [property: JsonPropertyName("stream")] string Stream,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("ansi")] string? Ansi = null,
    [property: JsonPropertyName("plain")] string? Plain = null
);

// Code Editor Payloads
public record EditorTarget(
    [property: JsonPropertyName("dbref")] string Dbref,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("member")] string Member,
    [property: JsonPropertyName("memberType")] string MemberType
);

public record EditorOpenPayload(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("target")] EditorTarget Target,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("headers")] string[] Headers,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("readOnly")] bool ReadOnly
);

public record EditorSavePayload(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("code")] string Code
);

public record EditorDiagnostic(
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("col")] int Col,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("severity")] string Severity
);

public record EditorResultPayload(
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("diagnostics")] EditorDiagnostic[] Diagnostics
);

// Window and Dock Payloads
public record WindowDimensions(
    [property: JsonPropertyName("width")] int? Width = null,
    [property: JsonPropertyName("minWidth")] int? MinWidth = null,
    [property: JsonPropertyName("height")] int? Height = null,
    [property: JsonPropertyName("minHeight")] int? MinHeight = null
);

public record WindowUpdatePayload(
    [property: JsonPropertyName("windowId")] string WindowId,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("html")] string Html,
    [property: JsonPropertyName("cssUrl")] string? CssUrl = null,
    [property: JsonPropertyName("dimensions")] WindowDimensions? Dimensions = null
);

// Styling and Audio Payloads
public record StyleLoadPayload(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("scope")] string Scope = "global"
);

public record AudioPlayPayload(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("volume")] double Volume = 1.0,
    [property: JsonPropertyName("loop")] bool Loop = false
);
