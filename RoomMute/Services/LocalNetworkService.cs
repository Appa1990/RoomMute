using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace RoomMute.Services;

public sealed record LocalAddress(string Address, string Adapter)
{
    public override string ToString() => Address;
    public string Label => $"{Address} · {Adapter}";
}
public static class LocalNetworkService
{
    // Enumerate local adapters only; no remote lookup and no packets are sent.
    public static IReadOnlyList<LocalAddress> GetAddresses(string partnerIp)
    {
        IPAddress.TryParse(partnerIp, out var partner);
        var results = new List<(LocalAddress Address, int Rank)>();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            var properties = adapter.GetIPProperties();
            foreach (var entry in properties.UnicastAddresses)
            {
                if (entry.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(entry.Address)) continue;
                bool sameSubnet = partner?.AddressFamily == AddressFamily.InterNetwork &&
                    SameSubnet(entry.Address, partner, entry.IPv4Mask);
                int rank = sameSubnet ? 0 : properties.GatewayAddresses.Any(g => !g.Address.Equals(IPAddress.Any)) ? 1 : 2;
                results.Add((new(entry.Address.ToString(), adapter.Name), rank));
            }
        }
        return results.OrderBy(x => x.Rank).ThenBy(x => x.Address.Adapter, StringComparer.Ordinal)
            .ThenBy(x => x.Address.Address, StringComparer.Ordinal).Select(x => x.Address)
            .DistinctBy(x => x.Address).ToList();
    }
    private static bool SameSubnet(IPAddress a, IPAddress b, IPAddress mask)
    {
        byte[] aa = a.GetAddressBytes(), bb = b.GetAddressBytes(), mm = mask.GetAddressBytes();
        return aa.Length == 4 && bb.Length == 4 && mm.Length == 4 &&
            Enumerable.Range(0, 4).All(i => (aa[i] & mm[i]) == (bb[i] & mm[i]));
    }
}

