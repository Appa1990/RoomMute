using RoomMute.Core;
using System.Text.Json;

if (args.Length == 2 && args[0] == "--verify-release")
{
    await RoomMute.Tests.UpdateIntegration.VerifyRealRelease(args[1]);
    return;
}
if (args.Length == 2 && args[0] == "--verify-detection-wave") { RoomMute.Tests.DetectionWaveTest.Run(args[1]); return; }
if (args.Length == 2 && args[0] == "--verify-noise-wave") { RoomMute.Tests.DetectionWaveTest.Run(args[1], false); return; }
int passed = 0;
void Check(string name, Action test)
{
    try { test(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception e) { Console.Error.WriteLine("FAIL " + name + ": " + e.Message); Environment.Exit(1); }
}
void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
var config = new AppConfig();
Check("VAD requires sustained attack and resets interrupted attack", () =>
{
    var vad = new VoiceActivityDetector();
    Assert(!vad.Update(-30, 0, 1000, config));
    Assert(!vad.Update(-50, 30, 1030, config));
    Assert(!vad.Update(-30, 40, 1040, config));
    Assert(!vad.Update(-30, 79, 1079, config));
    Assert(vad.Update(-30, 80, 1080, config) && vad.StartedAt == 1040);
});
Check("VAD hysteresis and release are monotonic", () =>
{
    var vad = new VoiceActivityDetector();
    vad.Update(-20, 0, 1000, config); vad.Update(-20, 40, 1040, config);
    Assert(!vad.Update(-39, 100, 500, config) && vad.Speaking);
    vad.Update(-50, 150, 550, config);
    Assert(!vad.Update(-50, 499, 899, config));
    Assert(vad.Update(-50, 500, 900, config) && !vad.Speaking);
});
Check("Short pause does not split speaking turn", () =>
{
    var vad = new VoiceActivityDetector();
    vad.Update(-20, 0, 1000, config); vad.Update(-20, 40, 1040, config);
    vad.Update(-60, 100, 1100, config); vad.Update(-30, 400, 1400, config);
    Assert(!vad.Update(-60, 450, 1450, config) && vad.Speaking);
});
Check("First speaker wins; tie priority then stable ID", () =>
{
    Assert(!Arbitration.RemoteWins(new(true, 1000, 20, "a"), new(true, 1180, 10, "b"), true));
    Assert(Arbitration.RemoteWins(new(true, 1000, 20, "a"), new(true, 1020, 10, "b"), true));
    Assert(!Arbitration.RemoteWins(new(true, 1000, 10, "a"), new(true, 1020, 10, "b"), true));
});
Check("Two peers never select each other for the same simultaneous snapshot", () =>
{
    for (int offset = -200; offset <= 200; offset++)
    for (int priority = 9; priority <= 11; priority++)
    {
        var a = new SpeakingState(true, 1000, 10, "a");
        var b = new SpeakingState(true, 1000 + offset, priority, "b");
        Assert(Arbitration.RemoteWins(a, b, true) != Arbitration.RemoteWins(b, a, true));
    }
});
Check("Standard mode always yields to speaking partner", () =>
{
    Assert(Arbitration.RemoteWins(new(true, 100, 1, "a"), new(true, 500, 20, "b"), false));
    Assert(!Arbitration.RemoteWins(new(true, 100, 1, "a"), new(false, 0, 20, "b"), false));
});
Check("Owned mute release waits and cancels if remote returns", () =>
{
    var mic = new FakeMicrophone(); var service = new AutoMuteService(mic);
    var local = new SpeakingState(false, 0, 10, "a");
    var remote = new SpeakingState(true, 100, 20, "b");
    service.Update(true, true, local, remote, true, 350, 0); Assert(mic.AttenuatedByRoomMute);
    service.Update(true, true, local, remote with { Speaking = false }, true, 350, 100);
    service.Update(true, true, local, remote with { Speaking = false }, true, 350, 449); Assert(mic.Muted);
    service.Update(true, true, local, remote, true, 350, 450); Assert(mic.Muted);
    service.Update(true, true, local, remote with { Speaking = false }, true, 350, 500);
    service.Update(true, true, local, remote with { Speaking = false }, true, 350, 850); Assert(!mic.Muted);
});
Check("Disconnect and disable restore immediately, ignoring release delay", () =>
{
    foreach (bool connected in new[] { true, false })
    {
        var mic = new FakeMicrophone(); var service = new AutoMuteService(mic);
        var remote = new SpeakingState(true, 100, 10, "b");
        service.Update(true, true, default, remote, true, 350, 0);
        service.Update(!connected, connected, default, remote, true, 350, 1);
        Assert(!mic.Muted);
    }
});
Check("Already manual mute survives automation stop", () =>
{
    var mic = new FakeMicrophone { Manual = true };
    var service = new AutoMuteService(mic);
    service.Update(true, true, default, new(true, 1, 1, "b"), true, 350, 0);
    Assert(mic.Muted && !mic.AttenuatedByRoomMute);
    service.Stop(); Assert(mic.Muted);
});
var ownId = Guid.NewGuid().ToString("N");
var ownSession = Guid.NewGuid().ToString("N");
var remoteId = Guid.NewGuid().ToString("N");
var remoteSession = Guid.NewGuid().ToString("N");
NetworkMessage Message(long sequence, string type = "state") => new()
{
    ClientId = remoteId, ClientName = "Partner", SessionId = remoteSession,
    EchoSessionId = ownSession, Sequence = sequence, Type = type, Speaking = true, StartedAt = 1000
};
Check("Handshake required before any mute decision", () =>
{
    var peer = new PeerState(ownId, ownSession);
    Assert(!peer.Accept(Message(1), 0));
    Assert(peer.Accept(Message(1, "hello") with { EchoSessionId = "" }, 0) && !peer.Connected);
    Assert(peer.Accept(Message(2), 10) && peer.Connected);
});
Check("Reordered/duplicate packets do not overwrite state or extend timeout", () =>
{
    var peer = new PeerState(ownId, ownSession);
    peer.Accept(Message(1, "hello"), 0);
    peer.Accept(Message(3) with { Speaking = false }, 100);
    Assert(!peer.Accept(Message(2), 200) && !peer.Current!.Speaking && peer.LastSeen == 100);
    Assert(!peer.Accept(Message(3), 400));
    Assert(!peer.CheckTimeout(3099, 3000));
    Assert(peer.CheckTimeout(3100, 3000) && !peer.Connected);
});
Check("Full-state heartbeat recovers lost start/stop packets", () =>
{
    var peer = new PeerState(ownId, ownSession);
    peer.Accept(Message(1, "hello") with { Speaking = false }, 0);
    peer.Accept(Message(3), 500); Assert(peer.Current!.Speaking);
    peer.Accept(Message(5) with { Speaking = false }, 1000); Assert(!peer.Current!.Speaking);
});
Check("Restarted peer replaces session; late old session cannot return", () =>
{
    var peer = new PeerState(ownId, ownSession);
    peer.Accept(Message(100, "hello"), 0);
    var restarted = Message(1, "hello") with { SessionId = Guid.NewGuid().ToString("N") };
    Assert(peer.Accept(restarted, 20));
    Assert(!peer.Accept(Message(101, "hello"), 30));
    Assert(peer.Current!.SessionId == restarted.SessionId);
});
Check("Timeout permits fresh state reconnect; goodbye disconnects", () =>
{
    var peer = new PeerState(ownId, ownSession);
    peer.Accept(Message(1, "hello"), 0);
    peer.CheckTimeout(3000, 3000);
    Assert(peer.Accept(Message(3), 3200) && peer.Connected);
    peer.Accept(Message(4, "bye") with { Speaking = false }, 3300);
    Assert(!peer.Connected);
});
Check("Invalid protocol, malformed identity and own packets are ignored", () =>
{
    var peer = new PeerState(ownId, ownSession);
    Assert(!peer.Accept(Message(1, "hello") with { ClientId = ownId }, 0));
    Assert(!peer.Accept(Message(1, "hello") with { Version = 5 }, 0));
    Assert(!peer.Accept(Message(1, "hello") with { StartedAt = 0 }, 0));
    Assert(!peer.Accept(Message(1, "hello") with { SessionId = "bad" }, 0));
});
Check("Wire roundtrip carries complete state", () =>
{
    var original = Message(2);
    var restored = JsonSerializer.Deserialize<NetworkMessage>(JsonSerializer.Serialize(original, NetworkMessage.JsonOptions), NetworkMessage.JsonOptions);
    Assert(restored == original);
});
Check("Configuration rejects invalid hysteresis and unsafe timer values", () =>
{
    Assert(config.Validate(false) == null);
    Assert(new AppConfig { SpeakThreshold = -50, SilenceThreshold = -40 }.Validate(false) != null);
    Assert(new AppConfig { SpeakThreshold = double.NaN }.Validate(false) != null);
    Assert(new AppConfig { PartnerIp = "not-an-ip" }.Validate() != null);
    Assert(new AppConfig { PartnerTimeout = 100 }.Validate(false) != null);
});

Check("Real mute ownership preserves pre-existing manual mute", () =>
{
    bool endpoint = true;
    int writes = 0;
    var mic = new MuteOwnership(() => endpoint, value => { endpoint = value; writes++; });
    mic.AttenuateAutomatically(); mic.Restore();
    Assert(endpoint && writes == 0 && !mic.AttenuatedByRoomMute);
});
Check("External manual mute supersedes an automatic mute", () =>
{
    bool endpoint = false;
    var mic = new MuteOwnership(() => endpoint, value => endpoint = value);
    mic.AttenuateAutomatically(); Assert(endpoint && mic.AttenuatedByRoomMute);
    mic.ExternalChange(); mic.Restore();
    Assert(endpoint && !mic.AttenuatedByRoomMute);
});
Check("External unmute is not immediately remuted", () =>
{
    bool endpoint = false;
    var mic = new MuteOwnership(() => endpoint, value => endpoint = value);
    mic.AttenuateAutomatically();
    endpoint = false; mic.ExternalChange();
    mic.AttenuateAutomatically(); Assert(!endpoint);
    mic.AllowAutomation(); mic.AttenuateAutomatically(); Assert(endpoint);
});
Check("Failed restore retains ownership for retry", () =>
{
    bool endpoint = false, fail = false;
    var mic = new MuteOwnership(() => endpoint, value =>
    {
        if (fail) throw new IOException("device temporarily unavailable");
        endpoint = value;
    });
    mic.AttenuateAutomatically(); fail = true;
    try { mic.Restore(); throw new Exception("Expected endpoint failure"); } catch (IOException) { }
    Assert(mic.AttenuatedByRoomMute);
    fail = false; mic.Restore(); Assert(!endpoint && !mic.AttenuatedByRoomMute);
});
Check("Failed mute never acquires restoration ownership", () =>
{
    var mic = new MuteOwnership(() => false, _ => throw new IOException());
    try { mic.AttenuateAutomatically(); } catch (IOException) { }
    Assert(!mic.AttenuatedByRoomMute);
});
Check("Tray manual mute is not restored by automation", () =>
{
    bool endpoint = false;
    var mic = new MuteOwnership(() => endpoint, value => endpoint = value);
    mic.AttenuateAutomatically(); mic.ManualMute(); mic.Restore();
    Assert(endpoint && !mic.AttenuatedByRoomMute);
});
try
{
    await NetworkIntegration.Run();
    passed++;
    Console.WriteLine("PASS Bidirectional UDP loopback, endpoint filter and clean cancellation");
}
catch (Exception e) { Console.Error.WriteLine("FAIL UDP integration: " + e); Environment.Exit(1); }


Check("Ducking reduces relative to baseline without compounding", () =>
{
    float volume = .8f; bool muted = false;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => muted) { RemainingPercent = 20 };
    duck.AttenuateAutomatically(); Assert(Math.Abs(volume - .16f) < .0001 && !muted);
    duck.AttenuateAutomatically(); Assert(Math.Abs(volume - .16f) < .0001);
    duck.Restore(); Assert(Math.Abs(volume - .8f) < .0001 && !muted);
});
Check("Ducking never changes a manually muted microphone", () =>
{
    float volume = .8f;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => true);
    duck.AttenuateAutomatically(); duck.Restore();
    Assert(volume == .8f && !duck.AttenuatedByRoomMute);
});
Check("External volume choice wins over stored baseline", () =>
{
    float volume = .8f;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => false);
    duck.AttenuateAutomatically(); volume = .5f; duck.ExternalChange(); duck.Restore();
    Assert(volume == .5f && !duck.AttenuatedByRoomMute);
    duck.AttenuateAutomatically(); Assert(volume == .5f);
    duck.AllowAutomation(); duck.AttenuateAutomatically(); duck.Restore(); Assert(volume == .5f);
});
Check("External mute during ducking restores volume but retains mute", () =>
{
    float volume = .8f; bool muted = false;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => muted);
    duck.AttenuateAutomatically(); muted = true; duck.ExternalChange(); duck.Restore();
    Assert(volume == .8f && muted);
});
Check("Changed volume is respected even before notification arrives", () =>
{
    float volume = .8f;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => false);
    duck.AttenuateAutomatically(); volume = .6f; duck.Restore(); Assert(volume == .6f);
});
Check("Ducking release delay and timeout restore the original volume", () =>
{
    float volume = .7f;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => false);
    var service = new AutoMuteService(duck);
    service.Update(true, true, default, new(true, 100, 10, "b"), true, 350, 0);
    Assert(Math.Abs(volume - .14f) < .0001);
    service.Update(true, true, default, default, true, 350, 100);
    service.Update(true, true, default, default, true, 350, 449);
    Assert(duck.AttenuatedByRoomMute);
    service.Update(true, true, default, default, true, 350, 450); Assert(volume == .7f);
    service.Update(true, true, default, new(true, 100, 10, "b"), true, 350, 500);
    service.Update(true, false, default, default, true, 350, 501); Assert(volume == .7f);
});
Check("Ducking handles quantized endpoint volume", () =>
{
    float volume = .83f;
    var duck = new DuckOwnership(() => volume, v => volume = (float)Math.Round(v, 2), () => false) { RemainingPercent = 17 };
    duck.AttenuateAutomatically(); Assert(volume == .14f);
    duck.Restore(); Assert(volume == .83f);
});
Check("Failed duck restore retains ownership for retry", () =>
{
    float volume = .8f; bool fail = false;
    var duck = new DuckOwnership(() => volume, v => { if (fail) throw new IOException(); volume = v; }, () => false);
    duck.AttenuateAutomatically(); fail = true;
    try { duck.Restore(); } catch (IOException) { }
    Assert(duck.AttenuatedByRoomMute);
    fail = false; duck.Restore(); Assert(volume == .8f);
});
Check("100 percent and zero original input do not acquire ownership", () =>
{
    float volume = .8f;
    var duck = new DuckOwnership(() => volume, v => volume = v, () => false) { RemainingPercent = 100 };
    duck.AttenuateAutomatically(); Assert(volume == .8f && !duck.AttenuatedByRoomMute);
    volume = 0; duck.RemainingPercent = 20; duck.AttenuateAutomatically(); Assert(!duck.AttenuatedByRoomMute);
});
Check("Old settings get default ducking; invalid slider values are rejected", () =>
{
    var old = JsonSerializer.Deserialize<AppConfig>("{}", NetworkMessage.JsonOptions)!;
    Assert(old.DuckPercent == 20);
    foreach (double value in new[] { 0d, 101d, double.NaN })
        Assert(new AppConfig { DuckPercent = value }.Validate(false) != null);
});


Check("Language preference survives JSON and old configurations default to German", () =>
{
    var old = JsonSerializer.Deserialize<AppConfig>("{}", NetworkMessage.JsonOptions)!;
    Assert(old.Language == "de");
    var serialized = JsonSerializer.Serialize(new AppConfig { Language = "en" }, NetworkMessage.JsonOptions);
    Assert(JsonSerializer.Deserialize<AppConfig>(serialized, NetworkMessage.JsonOptions)!.Language == "en");
    Assert(new AppConfig { Language = "fr" }.Validate(false) != null);
});
Check("Both languages have complete labels, tooltips and valid formatted messages", () =>
{
    var text = new UiText();
    foreach (string language in new[] { "de", "en" })
    {
        text.Language = language;
        foreach (string key in UiText.Keys) Assert(!string.IsNullOrWhiteSpace(text[key]));
        Assert(text.Format("Connected", "Partner-PC").Contains("Partner-PC"));
        Assert(text.Format("Ducking", 20).Contains("20"));
        Assert(text.Format("InvalidNumber", "Port").Contains("Port"));
        Assert(text["ReleaseTip"].Contains("700"));
    }
    Assert(text["Silence"] == "Silence threshold");
    Assert(text.Translate("Die Ruhe-Schwelle muss unter der Sprech-Schwelle liegen.").StartsWith("The silence"));
});
Check("Language changes notify existing bindings without changing protocol state", () =>
{
    var text = new UiText();
    var changes = new List<string?>();
    text.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
    var wireBefore = JsonSerializer.Serialize(Message(1), NetworkMessage.JsonOptions);
    text.Language = "en";
    Assert(changes.Contains("Item[]") && changes.Contains("Language"));
    Assert(wireBefore == JsonSerializer.Serialize(Message(1), NetworkMessage.JsonOptions));
    text.Language = "de";
    Assert(text["Apply"] == "Änderungen übernehmen");
});
Check("Local adapter list contains unique non-loopback IPv4 addresses", () =>
{
    var addresses = RoomMute.Services.LocalNetworkService.GetAddresses("192.0.2.34");
    Assert(addresses.Select(x => x.Address).Distinct().Count() == addresses.Count);
    foreach (var item in addresses)
    {
        Assert(System.Net.IPAddress.TryParse(item.Address, out var ip));
        Assert(ip!.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        Assert(!System.Net.IPAddress.IsLoopback(ip));
        Assert(!string.IsNullOrWhiteSpace(item.Adapter));
    }
});


Check("Push-to-talk immediately restores the ducked input and re-ducks on release", () =>
{
    float volume = .8f;
    var mic = new DuckOwnership(() => volume, v => volume = v, () => false);
    var service = new AutoMuteService(mic);
    var remote = new SpeakingState(true, 100, 10, "remote");
    service.Update(true, true, default, remote, true, 350, 0);
    Assert(volume < .8f);
    service.Update(true, true, default, remote, true, 350, 1, overrideHeld: true);
    Assert(volume == .8f && !service.PartnerHasPriority && !mic.AttenuatedByRoomMute);
    service.Update(true, true, default, remote, true, 350, 2, overrideHeld: true);
    Assert(volume == .8f);
    service.Update(true, true, default, remote, true, 350, 3);
    Assert(volume < .8f && service.PartnerHasPriority);
});
Check("Push-to-talk does not unmute or overwrite an externally chosen volume", () =>
{
    float volume = .8f; bool muted = false;
    var mic = new DuckOwnership(() => volume, v => volume = v, () => muted);
    var service = new AutoMuteService(mic);
    var remote = new SpeakingState(true, 100, 10, "remote");
    service.Update(true, true, default, remote, true, 350, 0);
    volume = .6f; mic.ExternalChange(); muted = true;
    service.Update(true, true, default, remote, true, 350, 1, overrideHeld: true);
    Assert(volume == .6f && muted);
});
Check("Push-to-talk clears a pending release timer and leaves silence open", () =>
{
    float volume = .8f;
    var mic = new DuckOwnership(() => volume, v => volume = v, () => false);
    var service = new AutoMuteService(mic);
    service.Update(true, true, default, new(true, 100, 10, "remote"), true, 350, 0);
    service.Update(true, true, default, default, true, 350, 10);
    service.Update(true, true, default, default, true, 350, 20, overrideHeld: true);
    service.Update(true, true, default, default, true, 350, 21);
    Assert(volume == .8f);
});
Check("Stop and disconnect safely restore during push-to-talk", () =>
{
    float volume = .8f;
    var mic = new DuckOwnership(() => volume, v => volume = v, () => false);
    var service = new AutoMuteService(mic);
    service.Update(true, true, default, new(true, 100, 10, "remote"), true, 350, 0);
    service.Update(true, false, default, default, true, 350, 1, overrideHeld: true);
    service.Stop(); Assert(volume == .8f);
});
Check("Automatic startup requires the saved microphone and valid partner configuration", () =>
{
    Assert(!AutoStartPolicy.IsConfigured(new AppConfig()));
    Assert(!AutoStartPolicy.IsConfigured(new AppConfig { PartnerIp = "192.0.2.34" }));
    Assert(AutoStartPolicy.IsConfigured(new AppConfig { PartnerIp = "192.0.2.34", AudioDeviceId = "saved-device" }));
    Assert(!AutoStartPolicy.IsConfigured(new AppConfig { PartnerIp = "invalid", AudioDeviceId = "saved-device" }));
});
Check("Startup retry waits with bounded backoff and explicit Stop cancels it", () =>
{
    var policy = new AutoStartPolicy();
    policy.Request(100);
    Assert(policy.IsDue(100));
    policy.Failed(100); Assert(!policy.IsDue(2099) && policy.IsDue(2100));
    policy.Failed(2100); Assert(policy.NextAttemptAt == 7100);
    policy.Failed(7100); Assert(policy.NextAttemptAt == 17100);
    policy.Failed(17100); Assert(policy.NextAttemptAt == 47100);
    policy.Failed(47100); Assert(policy.NextAttemptAt == 77100);
    policy.Cancel(); Assert(!policy.Requested && !policy.IsDue(long.MaxValue));
});
Check("Successful startup retains restart intent and resets error backoff", () =>
{
    var policy = new AutoStartPolicy();
    policy.Request(0); policy.Failed(0); policy.Failed(2000);
    policy.Succeeded(); Assert(policy.Requested && !policy.IsDue(long.MaxValue));
    policy.Failed(10000); Assert(policy.NextAttemptAt == 12000);
    policy.Cancel(); policy.Failed(20000); Assert(policy.NextAttemptAt == null);
});
Check("Shortcut gestures roundtrip letters, digits, function and numpad keys", () =>
{
    foreach (var value in new[] { "A", "5", "Ctrl+5", "Ctrl+Alt+Shift+Win+Num5", "F1", "F24", "OemPlus", "Alt+Space", "None" })
    {
        Assert(ShortcutGesture.TryParse(value, out var gesture));
        Assert(ShortcutGesture.TryParse(gesture.Serialize(), out var restored) && gesture == restored);
        Assert(new AppConfig { PushToTalkKey = value }.Validate(false) == null);
    }
    Assert(ShortcutGesture.TryParse("strg + 5", out var german) && german.Serialize() == "Ctrl+5");
    Assert(german.Display("de") == "Strg + 5" && german.Display("en") == "Ctrl + 5");
    var config = new AppConfig { PushToTalkKey = "Ctrl+Alt+5" };
    Assert(JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config))!.PushToTalkKey == config.PushToTalkKey);
});
Check("Shortcut parser rejects malformed, modifier-only and reserved bindings", () =>
{
    foreach (var value in new[] { "", "Ctrl", "Ctrl+", "Ctrl+Ctrl+A", "Fn+5", "Ctrl+F12", "F25", "Num10", "Ctrl+Mouse1", "None+A", "A+B" })
        Assert(!ShortcutGesture.TryParse(value, out _));
});
Check("Held combinations release on either main key or any modifier", () =>
{
    Assert(ShortcutGesture.TryParse("Ctrl+Alt+Shift+Win+5", out var chord));
    var down = new HashSet<int> { 0x35, 0x11, 0x12, 0x10, 0x5C };
    Assert(chord.IsHeld(down.Contains));
    foreach (int key in down.ToArray())
    {
        down.Remove(key); Assert(!chord.IsHeld(down.Contains)); down.Add(key);
    }
    Assert(!default(ShortcutGesture).IsHeld(_ => true));
});
Check("Recorder ignores modifier-only presses and supports Escape cancellation", () =>
{
    var recorder = new ShortcutRecorder();
    Assert(!recorder.Press(0xA2, 2) && recorder.Result == null);
    Assert(recorder.Press(0x35, 2) && recorder.Result == "Ctrl+5");
    Assert(!recorder.Press(0x41, 0) && recorder.Result == "Ctrl+5");
    recorder = new(); Assert(!recorder.Press(0x7B, 0));
    Assert(recorder.Press(0x1B, 0) && recorder.Canceled && recorder.Result == null);
    Assert(!recorder.Press(0x35, 0));
    recorder = new(); Assert(recorder.Press(0x41, 0) && recorder.Result == "A");
});
Check("Mouse3 gestures persist alone and with modifiers", () =>
{
    foreach (string value in new[] { "Mouse3", "Ctrl+Mouse3", "Alt+Shift+Mouse3" })
    {
        Assert(ShortcutGesture.TryParse(value, out var gesture) && gesture.IsMouse);
        Assert(gesture.Serialize() == value && new AppConfig { PushToTalkKey = value }.Validate(false) == null);
        var restored = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(new AppConfig { PushToTalkKey = value }))!;
        Assert(restored.PushToTalkKey == value);
    }
    Assert(ShortcutGesture.TryParse("mouse3", out var lower) && lower.VirtualKey == 4);
});
Check("Mouse3 recording ignores left and right buttons", () =>
{
    var recorder = new ShortcutRecorder();
    Assert(!recorder.Press(1, 0) && !recorder.Press(2, 0));
    Assert(recorder.Press(4, 2) && recorder.Result == "Ctrl+Mouse3");
});
Check("Mouse3 held combination ends on mouse or modifier release", () =>
{
    Assert(ShortcutGesture.TryParse("Ctrl+Mouse3", out var gesture));
    var down = new HashSet<int> { 4, 0x11 };
    Assert(gesture.IsHeld(down.Contains));
    down.Remove(4); Assert(!gesture.IsHeld(down.Contains));
    down.Add(4); down.Remove(0x11); Assert(!gesture.IsHeld(down.Contains));
});
Check("Shutdown recovery restores a saved duck after creating a new journal instance", () =>
{
    using var fixture = new RecoveryFixture();
    var duck = fixture.Duck(); duck.AttenuateAutomatically();
    Assert(Math.Abs(fixture.Volume - .16f) < .001);
    var reopened = new VolumeRecoveryJournal(fixture.Directory);
    Assert(reopened.Recover(reopened.ReadAll().Single(), () => fixture.Volume, value => fixture.Volume = value));
    Assert(Math.Abs(fixture.Volume - .8f) < .001 && reopened.ReadAll().Count == 0);
});
Check("Recovery preserves a volume changed since the previous session", () =>
{
    using var fixture = new RecoveryFixture(); fixture.Duck().AttenuateAutomatically(); fixture.Volume = .6f;
    Assert(!fixture.Journal.Recover(fixture.Journal.ReadAll().Single(), () => fixture.Volume, value => fixture.Volume = value));
    Assert(fixture.Volume == .6f && fixture.Journal.ReadAll().Count == 0);
});
Check("Failed startup recovery retains its record for the next retry", () =>
{
    using var fixture = new RecoveryFixture(); fixture.Duck().AttenuateAutomatically(); bool failed = false;
    try { fixture.Journal.Recover(fixture.Journal.ReadAll().Single(), () => fixture.Volume, _ => throw new IOException()); }
    catch (IOException) { failed = true; }
    Assert(failed && fixture.Journal.ReadAll().Count == 1);
});
Check("Recovery never changes manual mute", () =>
{
    using var fixture = new RecoveryFixture(); fixture.Duck().AttenuateAutomatically(); fixture.Muted = true;
    fixture.Journal.Recover(fixture.Journal.ReadAll().Single(), () => fixture.Volume, value => fixture.Volume = value);
    Assert(fixture.Muted && fixture.Volume == .8f);
});
Check("Failed write-ahead persistence prevents microphone reduction", () =>
{
    float volume = .8f;
    var duck = new DuckOwnership(() => volume, value => volume = value, () => false, (_, _) => throw new IOException());
    try { duck.AttenuateAutomatically(); } catch (IOException) { }
    Assert(volume == .8f && !duck.AttenuatedByRoomMute);
});
Check("Normal restore and external volume choice remove stale recovery records", () =>
{
    using var fixture = new RecoveryFixture(); var duck = fixture.Duck();
    duck.AttenuateAutomatically(); duck.Restore(); Assert(fixture.Journal.ReadAll().Count == 0);
    duck.AttenuateAutomatically(); fixture.Volume = .7f; duck.ExternalChange();
    Assert(fixture.Volume == .7f && fixture.Journal.ReadAll().Count == 0);
});
Check("Failed regular restore retains ownership and persistent recovery record", () =>
{
    using var fixture = new RecoveryFixture(); bool fail = false;
    var duck = new DuckOwnership(() => fixture.Volume, value => { if (fail) throw new IOException(); fixture.Volume = value; }, () => false,
        (original, applied) => fixture.Journal.Save("test-device", original, applied), () => fixture.Journal.Clear("test-device"));
    duck.AttenuateAutomatically(); fail = true;
    try { duck.Restore(); } catch (IOException) { }
    Assert(duck.AttenuatedByRoomMute && fixture.Journal.ReadAll().Count == 1);
});
Check("Recovery stores the actual driver-quantized input level", () =>
{
    using var fixture = new RecoveryFixture();
    var duck = new DuckOwnership(() => fixture.Volume, value => fixture.Volume = (float)Math.Round(value, 1), () => false,
        (original, applied) => fixture.Journal.Save("test-device", original, applied), () => fixture.Journal.Clear("test-device"));
    duck.AttenuateAutomatically(); Assert(fixture.Journal.ReadAll().Single().Applied == fixture.Volume);
    fixture.Journal.Recover(fixture.Journal.ReadAll().Single(), () => fixture.Volume, value => fixture.Volume = value);
    Assert(fixture.Volume == .8f);
});
Check("Update versions normalize revision and reject downgrade", () =>
{
    var release = UpdateRelease.Parse(RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64), 100), "win-x64");
    Assert(release.NewerThan(new Version(1,5,0)) && !release.NewerThan(new Version(9,1,0,0)) && !release.NewerThan(new Version(10,0,0)));
});
Check("Update parser rejects foreign download URLs and missing verification hashes", () =>
{
    foreach (string json in new[] { RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64), 100, "https://example.com/tool.zip"),
        RoomMute.Tests.UpdateIntegration.ReleaseJson("", 100) })
    {
        bool failed = false; try { UpdateRelease.Parse(json,"win-x64"); } catch (InvalidDataException) { failed = true; }
        Assert(failed);
    }
});
Check("Update parser excludes prereleases and unsupported architecture", () =>
{
    bool failed = false;
    try { UpdateRelease.Parse(RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64), 100, prerelease:true),"win-x64"); }
    catch (InvalidDataException) { failed = true; } Assert(failed);
    failed = false;
    try { UpdateRelease.Parse(RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64),100),"win-arm64"); }
    catch (InvalidDataException) { failed = true; } Assert(failed);
});
Check("Update metadata retains plain release notes and bounds package size", () =>
{
    var release = UpdateRelease.Parse(RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64),100),"win-x64");
    Assert(release.Notes.Contains("Second line")); bool failed = false;
    try { UpdateRelease.Parse(RoomMute.Tests.UpdateIntegration.ReleaseJson(new string('a',64),long.MaxValue),"win-x64"); }
    catch (InvalidDataException) { failed = true; } Assert(failed);
});
Check("Legacy settings get F8 and reject reserved or unknown keys", () =>
{
    var old = JsonSerializer.Deserialize<AppConfig>("{}", NetworkMessage.JsonOptions)!;
    Assert(old.PushToTalkKey == "F8");
    foreach (string key in new[] { "None", "F6", "F7", "F8", "F9", "F10", "F11" })
        Assert(new AppConfig { PushToTalkKey = key }.Validate(false) == null);
    Assert(new AppConfig { PushToTalkKey = "F12" }.Validate(false) != null);
});

try
{
    await LatePartnerIntegration.Run();
    passed++;
    Console.WriteLine("PASS Actual UDP handshake with late partner and partner restart");
}
catch (Exception ex) { Console.Error.WriteLine("FAIL Late partner integration: " + ex); Environment.Exit(1); }
try { await RoomMute.Tests.UpdateIntegration.Run(); passed++; Console.WriteLine("PASS Verified update download, corruption rejection, cancellation and install re-verification"); }
catch (Exception error) { Console.Error.WriteLine("FAIL Update integration: " + error); Environment.Exit(1); }
passed += RoomMute.Tests.NoiseFilterTests.Run();
Console.WriteLine($"{passed} tests passed.");

sealed class FakeMicrophone : IMicrophone
{
    public bool Manual;
    public bool AttenuatedByRoomMute { get; private set; }
    public bool Muted => Manual || AttenuatedByRoomMute;
    public void AttenuateAutomatically() { if (!Muted) AttenuatedByRoomMute = true; }
    public void Restore() => AttenuatedByRoomMute = false;
}










sealed class RecoveryFixture : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "RoomMute-recovery-test-" + Guid.NewGuid().ToString("N"));
    public VolumeRecoveryJournal Journal { get; }
    public float Volume = .8f;
    public bool Muted;
    public RecoveryFixture() => Journal = new(Directory);
    public DuckOwnership Duck() => new(() => Volume, value => Volume = value, () => Muted,
        (original, applied) => Journal.Save("test-device", original, applied), () => Journal.Clear("test-device"));
    public void Dispose() { if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory,true); }
}
