using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
namespace RoomMute.Core;

public sealed record VolumeRecovery(string DeviceId, float Original, float Applied);

/// <summary>Write-ahead restoration state, retained until restore succeeds or an external choice supersedes it.</summary>
public sealed class VolumeRecoveryJournal(string directory)
{
    private string PathFor(string id) => Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id))) + ".json");
    public void Save(string id, float original, float applied)
    {
        Validate(new(id, original, applied));
        Directory.CreateDirectory(directory);
        string path = PathFor(id), temporary = path + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new VolumeRecovery(id, original, applied));
        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        { file.Write(bytes); file.Flush(true); }
        File.Move(temporary, path, true);
    }
    public void Clear(string id) => File.Delete(PathFor(id));
    public IReadOnlyList<VolumeRecovery> ReadAll(Action<Exception>? invalidRecord = null)
    {
        if (!Directory.Exists(directory)) return Array.Empty<VolumeRecovery>();
        var records = new List<VolumeRecovery>();
        foreach (string path in Directory.GetFiles(directory, "*.json"))
        {
            try
            {
            var record = JsonSerializer.Deserialize<VolumeRecovery>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid microphone recovery record.");
            Validate(record);
            if (Path.GetFullPath(path) != Path.GetFullPath(PathFor(record.DeviceId))) throw new InvalidDataException("Invalid recovery device identity.");
            records.Add(record);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
            { if (invalidRecord == null) throw; invalidRecord(error); }
        }
        return records;
    }
    private static void Validate(VolumeRecovery record)
    {
        if (string.IsNullOrWhiteSpace(record.DeviceId) || record.DeviceId.Length > 2048 ||
            !float.IsFinite(record.Original) || !float.IsFinite(record.Applied) ||
            record.Original is < 0 or > 1 || record.Applied is < 0 or > 1)
            throw new InvalidDataException("Invalid microphone recovery values.");
    }
    public bool Recover(VolumeRecovery record, Func<float> read, Action<float> write)
    {
        // Preserve any volume chosen after RoomMute's last recorded write. Never touch mute.
        bool matches = Math.Abs(read() - record.Applied) < 0.0001f;
        if (matches) write(record.Original);
        Clear(record.DeviceId); // An exception retains the record for a later retry.
        return matches;
    }
}


