using RoomMute.Core;
using RoomMute.Networking;
namespace RoomMute.Services;

/// <summary>Receive events are marshalled by the caller to the control thread.</summary>
public sealed class NetworkService : IDisposable
{
    private readonly UdpTransport transport;
    private readonly AppConfig config;
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public PeerState Peer { get; }
    private long sequence;
    public event Action<NetworkMessage>? Received;
    public event Action<Exception>? Failed;
    public NetworkService(AppConfig config, System.Net.IPAddress? bindAddress = null)
    {
        this.config = config.Copy();
        Peer = new(config.ClientId, SessionId);
        transport = new(config.PartnerIp, config.Port, bindAddress);
        transport.Received += message => Received?.Invoke(message);
        transport.Failed += error => Failed?.Invoke(error);
    }
    public void Start() => transport.Start();
    public void Send(string type, bool speaking, long startedAt, bool muted)
    {
        transport.Send(new()
        {
            Type = type, ClientId = config.ClientId, ClientName = config.ClientName,
            SessionId = SessionId, EchoSessionId = Peer.EchoSession, Sequence = ++sequence,
            Priority = config.Priority, Speaking = speaking, StartedAt = speaking ? startedAt : 0, Muted = muted
        });
    }
    public void Dispose() => transport.Dispose();
}


