using System.Text.Json;

namespace RoomMute.Core;

/// <summary>Wire DTO intentionally contains no audio buffers or audio payload fields.</summary>
public sealed record NetworkMessage
{
    public int Version { get; init; } = 1;
    public string Type { get; init; } = "state";
    public string ClientId { get; init; } = "";
    public string ClientName { get; init; } = "";
    public string SessionId { get; init; } = "";
    public string EchoSessionId { get; init; } = "";
    public long Sequence { get; init; }
    public int Priority { get; init; }
    public bool Speaking { get; init; }
    public long StartedAt { get; init; }
    public bool Muted { get; init; }
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public bool IsValid() =>
        Version == 1 && Type is "hello" or "heartbeat" or "speaking" or "state" or "bye" &&
        Guid.TryParseExact(ClientId, "N", out _) && Guid.TryParseExact(SessionId, "N", out _) &&
        ClientName is { Length: > 0 and <= 64 } && EchoSessionId is { Length: <= 64 } &&
        Sequence > 0 && Priority is >= 0 and <= 10000 && (!Speaking || StartedAt > 0);
}

/// <summary>Session handshake prevents old processes from reviving expired state.</summary>
public sealed class PeerState(string ownClientId, string ownSessionId)
{
    private readonly HashSet<string> retired = new(StringComparer.Ordinal);
    private string? session;
    private string? client;
    private long sequence;
    public NetworkMessage? Current { get; private set; }
    public long LastSeen { get; private set; }
    public bool Connected { get; private set; }
    public string EchoSession => session ?? "";

    public bool Accept(NetworkMessage message, long now)
    {
        if (!message.IsValid() || message.ClientId == ownClientId || retired.Contains(message.SessionId)) return false;
        if (session != message.SessionId)
        {
            if (message.Type != "hello") return false;
            // An already paired peer must acknowledge this process before replacing its session.
            if (session != null && message.EchoSessionId != ownSessionId) return false;
            if (session != null) retired.Add(session);
            session = message.SessionId;
            client = message.ClientId;
            sequence = 0;
            Connected = false;
            Current = null;
        }
        if (client != message.ClientId || message.Sequence <= sequence) return false;
        sequence = message.Sequence;
        if (message.EchoSessionId != ownSessionId) return true; // handshake only, never mute yet
        Current = message;
        LastSeen = now;
        Connected = message.Type != "bye";
        return true;
    }
    public bool CheckTimeout(long now, int timeout)
    {
        if (!Connected || now - LastSeen < timeout) return false;
        Connected = false;
        Current = null;
        return true;
    }
}
