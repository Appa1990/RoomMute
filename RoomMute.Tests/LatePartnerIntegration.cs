using System.Net;
using System.Net.Sockets;
using RoomMute.Core;
using RoomMute.Services;

static class LatePartnerIntegration
{
    public static async Task Run()
    {
        using var probe = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)probe.Client.LocalEndPoint!).Port;
        probe.Close();
        var aAddress = IPAddress.Parse("127.0.0.2");
        var bAddress = IPAddress.Parse("127.0.0.3");
        var aConfig = new AppConfig { PartnerIp = bAddress.ToString(), Port = port, ClientName = "Boot A" };
        var bConfig = new AppConfig { PartnerIp = aAddress.ToString(), Port = port, ClientName = "Boot B" };
        using var a = new NetworkService(aConfig, aAddress);
        NetworkService? b = null;
        var gate = new object();
        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Attach(NetworkService receiver)
        {
            receiver.Received += message =>
            {
                lock (gate)
                {
                    if (!ReferenceEquals(receiver, a) && !ReferenceEquals(receiver, b)) return;
                    if (receiver.Peer.Accept(message, Environment.TickCount64) && message.Type == "hello")
                        receiver.Send("state", false, 0, false);
                }
            };
            receiver.Failed += error => failure.TrySetResult(error);
        }
        async Task AwaitConnected()
        {
            for (int i = 0; i < 30; i++)
            {
                lock (gate)
                {
                    a.Send("hello", false, 0, false);
                    b!.Send("hello", false, 0, false);
                }
                await Task.Delay(75);
                if (failure.Task.IsCompleted) throw await failure.Task;
                lock (gate) { if (a.Peer.Connected && b!.Peer.Connected) return; }
            }
            throw new TimeoutException("Clients failed to connect automatically.");
        }
        try
        {
            Attach(a); a.Start();
            for (int i = 0; i < 3; i++)
            {
                a.Send("hello", false, 0, false);
                await Task.Delay(75);
            }
            if (failure.Task.IsCompleted) throw await failure.Task;
            b = new NetworkService(bConfig, bAddress);
            Attach(b); b.Start();
            await AwaitConnected();
            string oldSession = b.SessionId;
            lock (gate)
            {
                b.Dispose();
                a.Peer.CheckTimeout(a.Peer.LastSeen + 3000, 3000);
                b = new NetworkService(bConfig, bAddress);
                Attach(b); b.Start();
            }
            await AwaitConnected();
            lock (gate)
            {
                if (a.Peer.Current?.SessionId != b.SessionId || b.SessionId == oldSession)
                    throw new Exception("Restarted partner session was not adopted.");
            }
        }
        finally { lock (gate) b?.Dispose(); }
    }
}

