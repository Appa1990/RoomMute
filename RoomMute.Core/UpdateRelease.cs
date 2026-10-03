using System.Text.Json;
namespace RoomMute.Core;

public sealed record UpdateRelease(Version Version, string Tag, string Notes, Uri DownloadUrl, long Size, string Sha256)
{
    public static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
    public static UpdateRelease Parse(string json, string runtime)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) throw new InvalidDataException("No stable release.");
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!System.Version.TryParse(tag.TrimStart('v'), out var version)) throw new InvalidDataException("Invalid release version.");
        string expected = $"RoomMute-{runtime}-{tag}.zip";
        var assets = root.GetProperty("assets").EnumerateArray();
        var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == expected);
        if (asset.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("No package for this Windows architecture.");
        string digest = asset.TryGetProperty("digest", out var digestValue) ? digestValue.GetString() ?? "" : "";
        if (!digest.StartsWith("sha256:", StringComparison.Ordinal) || digest.Length != 71 || !digest[7..].All(Uri.IsHexDigit))
            throw new InvalidDataException("The release has no SHA-256 verification hash.");
        var url = new Uri(asset.GetProperty("browser_download_url").GetString()!);
        if (url.Scheme != "https" || url.Host != "github.com" || !url.AbsolutePath.StartsWith("/Appa1990/RoomMute/releases/download/", StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected update download source.");
        long size = asset.GetProperty("size").GetInt64();
        if (size <= 0 || size > 512L * 1024 * 1024) throw new InvalidDataException("Invalid update package size.");
        return new(Normalize(version), tag, root.GetProperty("body").GetString() ?? "", url, size, digest[7..].ToLowerInvariant());
    }
    public bool NewerThan(Version current) => Version > Normalize(current);
}
