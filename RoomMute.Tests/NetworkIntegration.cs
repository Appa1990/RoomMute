using System.Net;
using System.Net.Sockets;
using System.Text;
using RoomMute.Core;
using RoomMute.Networking;

static class NetworkIntegration
{
    public static async Task Run()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
        probe.Close();
        var aAddress = IPAddress.Parse("127.0.0.2");
        var bAddress = IPAddress.Parse("127.0.0.3");
        using var a = new UdpTransport(bAddress.ToString(), port, aAddress);
        using var b = new UdpTransport(aAddress.ToString(), port, bAddress);
        var receivedA = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedB = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        a.Received += m => receivedA.TrySetResult(m);
        b.Received += m => receivedB.TrySetResult(m);
        a.Failed += e => receivedA.TrySetException(e);
        b.Failed += e => receivedB.TrySetException(e);
        a.Start(); b.Start();
        NetworkMessage packet = new()
        {
            Type = "hello", ClientId = Guid.NewGuid().ToString("N"), SessionId = Guid.NewGuid().ToString("N"),
            ClientName = "Loopback A", Sequence = 1, Speaking = true, StartedAt = 12345
        };
        // A foreign source port and invalid JSON must not generate a valid state event.
        using var stranger = new UdpClient(new IPEndPoint(aAddress, 0));
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(packet, NetworkMessage.JsonOptions);
        await stranger.SendAsync(bytes, new IPEndPoint(bAddress, port));
        await stranger.SendAsync(Encoding.UTF8.GetBytes("{not json"), new IPEndPoint(bAddress, port));
        await Task.Delay(100);
        if (receivedB.Task.IsCompleted) throw new Exception("Foreign endpoint was accepted.");
        a.Send(packet);
        var incoming = await receivedB.Task.WaitAsync(TimeSpan.FromSeconds(3));
        if (incoming != packet) throw new Exception("Packet changed in transit.");
        b.Send(packet with { ClientName = "Loopback B", Speaking = false });
        var response = await receivedA.Task.WaitAsync(TimeSpan.FromSeconds(3));
        if (response.Speaking || response.ClientName != "Loopback B") throw new Exception("Return packet incorrect.");
        a.Dispose(); b.Dispose();
        await Task.WhenAll(a.Completion, b.Completion).WaitAsync(TimeSpan.FromSeconds(3));
    }
}
