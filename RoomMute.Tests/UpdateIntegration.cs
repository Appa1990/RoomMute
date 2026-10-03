using RoomMute.Core;
using RoomMute.Services;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
namespace RoomMute.Tests;

public static class UpdateIntegration
{
    public static string ReleaseJson(string digest, long size, string url = "https://github.com/Appa1990/RoomMute/releases/download/v9.1.0/RoomMute-win-x64-v9.1.0.zip", string tag = "v9.1.0", bool prerelease = false) => JsonSerializer.Serialize(new
    {
        draft = false, prerelease, tag_name = tag, body = "Patch notes\nSecond line",
        assets = new[] { new { name = "RoomMute-win-x64-" + tag + ".zip", digest = "sha256:" + digest, size, browser_download_url = url } }
    });
    public static async Task Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "RoomMute-update-test-" + Guid.NewGuid().ToString("N"));
        byte[] bytes = "Verified package test bytes"u8.ToArray();
        string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var release = UpdateRelease.Parse(ReleaseJson(hash, bytes.Length), "win-x64");
        try
        {
            using var service = new UpdateService(new Handler(bytes, ReleaseJson(hash, bytes.Length)), root);
            var discovered = await service.Check(CancellationToken.None);
            if (discovered.Tag != release.Tag || !discovered.Notes.Contains("Second line")) throw new Exception("Wrong update metadata");
            string package = await service.Download(release, new Progress<double>(), CancellationToken.None);
            if (!File.ReadAllBytes(package).SequenceEqual(bytes)) throw new Exception("Package changed");
            using var corrupted = new UpdateService(new Handler("Corrupted data"u8.ToArray(), ""), root);
            bool rejected = false;
            try { await corrupted.Download(release, new Progress<double>(), CancellationToken.None); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected || !File.ReadAllBytes(package).SequenceEqual(bytes)) throw new Exception("Corruption replaced verified package");
            using var wrongHash = new UpdateService(new Handler(bytes, ""), root);
            rejected = false;
            try { await wrongHash.Download(release with { Sha256 = new string('0', 64) }, new Progress<double>(), CancellationToken.None); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Wrong hash accepted");
            using var canceled = new CancellationTokenSource(); canceled.Cancel();
            try { await service.Download(release, new Progress<double>(), canceled.Token); throw new Exception("Canceled download continued"); }
            catch (OperationCanceledException) { }
            rejected = false;
            try { UpdateService.Extract(package, release with { Sha256 = new string('0', 64) }); }
            catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Tampered installation accepted");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    public static async Task VerifyRealRelease(string expectedTag)
    {
        string root = Path.Combine(Path.GetTempPath(), "RoomMute-live-update-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var service = new UpdateService(storageDirectory: root);
            var release = await service.Check(CancellationToken.None);
            if (release.Tag != expectedTag) throw new Exception("Latest release does not match expected version.");
            string package = await service.Download(release, new Progress<double>(), CancellationToken.None);
            string executable = UpdateService.Extract(package, release);
            if (!File.Exists(executable)) throw new Exception("No extracted application.");
            Console.WriteLine("PASS Live GitHub update metadata, release notes, complete download, SHA-256 and extracted version: " + expectedTag);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
    }
    private sealed class Handler(byte[] data, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = request.RequestUri!.Host == "api.github.com" ? new StringContent(json) : new ByteArrayContent(data) });
        }
    }
}

