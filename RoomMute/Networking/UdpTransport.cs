using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using RoomMute.Core;
namespace RoomMute.Networking;

public sealed class UdpTransport : IDisposable
{
    private readonly UdpClient socket;
    private readonly IPEndPoint partner;
    private readonly CancellationTokenSource cancellation = new();
    private bool disposed;
    public event Action<NetworkMessage>? Received;
    public event Action<Exception>? Failed;
    public Task Completion { get; private set; } = Task.CompletedTask;
    public UdpTransport(string ip, int port, IPAddress? localAddress = null)
    {
        partner = new(IPAddress.Parse(ip), port);
        socket = new UdpClient(AddressFamily.InterNetwork);
        try
        {
            socket.ExclusiveAddressUse = true;
            socket.Client.Bind(new IPEndPoint(localAddress ?? IPAddress.Any, port));
        }
        catch { socket.Dispose(); cancellation.Dispose(); throw; }
    }
    public void Start() => Completion = ReceiveLoop();
    public void Send(NetworkMessage message)
    {
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message, NetworkMessage.JsonOptions);
        socket.Send(payload, payload.Length, partner);
    }
    private async Task ReceiveLoop()
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                UdpReceiveResult packet;
                try { packet = await socket.ReceiveAsync(cancellation.Token).ConfigureAwait(false); }
                // Windows can surface ICMP "port unreachable" as a receive error while the partner is offline.
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset) { continue; }
                if (!packet.RemoteEndPoint.Equals(partner) || packet.Buffer.Length > 4096) continue;
                try
                {
                    var message = JsonSerializer.Deserialize<NetworkMessage>(packet.Buffer, NetworkMessage.JsonOptions);
                    if (message?.IsValid() == true) Received?.Invoke(message);
                }
                catch (JsonException) { }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) { if (!cancellation.IsCancellationRequested) Failed?.Invoke(ex); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        cancellation.Cancel();
        socket.Dispose();
        _ = Completion.ContinueWith(_ => cancellation.Dispose(), TaskScheduler.Default);
    }
}
