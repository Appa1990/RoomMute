using RoomMute.Core;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Runtime.InteropServices;
namespace RoomMute.Services;

public sealed class UpdateService : IDisposable
{
    public const string Repository = "https://github.com/Appa1990/RoomMute";
    private readonly HttpClient http;
    private readonly string storage;
    public string Runtime => RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
    public UpdateService(HttpMessageHandler? handler = null, string? storageDirectory = null)
    {
        storage = storageDirectory ?? Path.Combine(LogService.DataDirectory, "Updates");
        http = handler == null ? new HttpClient() : new HttpClient(handler);
        http.Timeout = TimeSpan.FromMinutes(15);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RoomMute-Updater/1.5");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }
    public async Task<UpdateRelease> Check(CancellationToken cancellation)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        string json = await http.GetStringAsync("https://api.github.com/repos/Appa1990/RoomMute/releases/latest", timeout.Token);
        return UpdateRelease.Parse(json, Runtime);
    }
    public async Task<string> Download(UpdateRelease release, IProgress<double> progress, CancellationToken cancellation)
    {
        string directory = Path.Combine(storage, release.Tag);
        Directory.CreateDirectory(directory);
        string destination = Path.Combine(directory, $"RoomMute-{Runtime}-{release.Tag}.zip");
        string partial = destination + ".part";
        using var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellation);
        response.EnsureSuccessStatusCode();
        await using (var input = await response.Content.ReadAsStreamAsync(cancellation))
        await using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            byte[] buffer = new byte[81920]; long total = 0; int read;
            while ((read = await input.ReadAsync(buffer, cancellation)) > 0)
            {
                total += read;
                if (total > release.Size) throw new InvalidDataException("Update is larger than declared.");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellation);
                progress.Report(total * 100d / release.Size);
            }
            if (total != release.Size) throw new InvalidDataException("Incomplete update download.");
        }
        await using (var file = File.OpenRead(partial))
        {
            string actual = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellation)).ToLowerInvariant();
            if (actual != release.Sha256) throw new InvalidDataException("Update verification failed (SHA-256).");
        }
        File.Move(partial, destination, true);
        return destination;
    }
    public static string Extract(string package, UpdateRelease release)
    {
        using (var input = File.OpenRead(package))
            if (Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant() != release.Sha256) throw new InvalidDataException("Update file changed after download.");
        string target = Path.Combine(Path.GetDirectoryName(package)!, "app-" + Guid.NewGuid().ToString("N"));
        ZipFile.ExtractToDirectory(package, target);
        string executable = Path.Combine(target, "RoomMute.exe");
        if (!File.Exists(executable) || !File.Exists(Path.Combine(target, "RoomMute.dll"))) throw new InvalidDataException("Update contains no RoomMute application.");
        var version = System.Reflection.AssemblyName.GetAssemblyName(Path.Combine(target, "RoomMute.dll")).Version;
        if (version == null || UpdateRelease.Normalize(version) != release.Version) throw new InvalidDataException("Update application version does not match the release.");
        return executable;
    }
    public void Dispose() => http.Dispose();
}
