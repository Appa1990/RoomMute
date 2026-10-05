using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using RoomMute.Services;
namespace RoomMute.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly LogService log;
    private readonly ConfigurationService configuration;
    private readonly MicrophoneService microphone;
    private readonly AudioMonitorService audio = new();
    private readonly AutoMuteService automation;
    private readonly VoiceActivityDetector detector = new();
    private readonly Dispatcher dispatcher = Application.Current.Dispatcher;
    private readonly DispatcherTimer timer;
    private readonly DispatcherTimer retryTimer;
    private readonly AutoStartPolicy autoStart = new();
    private GlobalPushToTalkService? pushToTalk;
    private bool initialized, shortcutAvailable, overrideHeld;
    private long overrideStartedAt;
    private NetworkService? network;
    private Action<Exception>? audioErrorHandler;
    private AppConfig config;
    private long nextState;
    private long nextHello;
    private int generation;
    private bool wasConnected;
    private bool lastPriority;
    private bool disposed;
    private bool running;
    private bool enabled;
    private double level = -96;
    private string notice = "Mikrofon wählen und die IP des Partner-PCs eintragen.";
    private string partnerName = "Noch kein Partner";
    private string microphoneName = "Kein Mikrofon ausgewählt";
    private bool actualMuted;
    private bool ownedMute;
    private bool remoteSpeaking;
    public ObservableCollection<AudioDevice> Devices { get; } = new();
    public SettingsViewModel Settings { get; private set; }
    public bool IsRunning => running;
    public bool AutomationEnabled => enabled;
    public bool MinimizeToTray => config.MinimizeToTray;
    public bool StartMinimized => config.StartMinimized;
    public UiText Text => UiText.Current;
    public IReadOnlyList<LanguageChoice> Languages { get; } = new[] { new LanguageChoice("de", "Deutsch"), new LanguageChoice("en", "English") };
    public string Language
    {
        get => Text.Language;
        set
        {
            if (Text.Language == value || value is not ("de" or "en")) return;
            var next = config.Copy();
            next.Language = value;
            try { configuration.Save(next); config.Language = value; Text.Language = value; }
            catch (Exception ex) { notice = ex.Message; Changed(nameof(Notice)); }
        }
    }
    public ObservableCollection<LocalAddress> LocalAddresses { get; } = new();
    private LocalAddress? selectedLocalAddress;
    public LocalAddress? SelectedLocalAddress
    {
        get => selectedLocalAddress;
        set { Set(ref selectedLocalAddress, value); Changed(nameof(LocalAddressTip)); CommandManagerRefresh(); }
    }
    private bool hasPendingChanges;
    public string LocalAddressTip => Text["IpTip"] + (SelectedLocalAddress == null ? "" : "\n\n" + SelectedLocalAddress.Label);
    public string PendingText => Text[hasPendingChanges ? "Pending" : "Applied"];
    public Brush PendingBrush => hasPendingChanges ? Brushes.Khaki : Brushes.SlateGray;
    public string Notice => Text.Translate(notice) + (autoStart.Requested && !running ? " · " + Text["AutoRetry"] : "");
    public string ConnectionText => wasConnected ? Text.Format("Connected", partnerName) : running ? Text[network == null ? "LocalTest" : "Waiting"] : Text[autoStart.Requested ? "AutoWaiting" : "Disconnected"];
    public string ConnectionDetail => running && network != null ? $"{config.PartnerIp} · UDP {config.Port}" : Text["LocalOnly"];
    public string PartnerName => wasConnected ? partnerName : Text["NoPartner"];
    public string MicrophoneName => microphone.HasDevice ? microphoneName : Text["NoMicrophone"];
    public string LocalState => Text[!running ? "Ready" : actualMuted ? "Muted" : detector.Speaking ? "Speaking" : "Silent"];
    public string RemoteState => Text[!wasConnected ? "Offline" : remoteSpeaking ? "Speaking" : "Silent"];
    public string ShortcutStatus => overrideHeld ? Text["PushActive"] : config.PushToTalkKey == "None" ? Text["PushOff"] : Text.Format(shortcutAvailable || !initialized ? "PushReady" : "PushUnavailable", ShortcutGesture.TryParse(config.PushToTalkKey, out var shortcut) ? shortcut.Display(Text.Language) : config.PushToTalkKey);
    public string MicrophoneState => !microphone.HasDevice ? Text["SelectMic"] : ownedMute ? Text.Format("Ducking", config.DuckPercent) : Text[actualMuted ? "ManualMute" : overrideHeld ? "PushActive" : "Active"];
    public string MicrophoneHint => Text[!microphone.HasDevice ? "SelectHint" : ownedMute ? "DuckHint" : actualMuted ? "MuteHint" : overrideHeld ? "PushHint" : running ? "ActiveHint" : "ReadyHint"];
    public string PriorityState => Text[!running || !wasConnected ? "WaitingPriority" : overrideHeld ? "PushActive" : automation.PartnerHasPriority ? "PartnerPriority" : detector.Speaking ? "YouPriority" : "NextTurn"];
    public string ModeText => Text[config.FirstSpeakerWins ? "FirstSpeaker" : "PartnerFirst"];
    public string SpeakingSince => detector.Speaking ? DateTimeOffset.FromUnixTimeMilliseconds(detector.StartedAt).ToLocalTime().ToString("HH:mm:ss.fff") : "—";
    public double Level => level;
    public double ActiveThreshold => config.SpeakThreshold;
    public string LevelText => level.ToString("0.0", Text.Culture) + " dBFS";
    public string AutomationText => Text[enabled ? "AutomationOn" : "AutomationOff"];
    public string ToggleText => Text[enabled ? "Pause" : "Enable"];

    public Brush ConnectionBrush => wasConnected ? Brushes.MediumAquamarine : Brushes.SlateGray;
    public Brush MicrophoneBrush => !microphone.HasDevice ? Brushes.SlateGray : ownedMute ? new SolidColorBrush(Color.FromRgb(255, 189, 105))
        : actualMuted ? new SolidColorBrush(Color.FromRgb(244, 126, 145)) : new SolidColorBrush(Color.FromRgb(93, 224, 180));
    public Brush LocalBrush => detector.Speaking && !actualMuted ? Brushes.MediumAquamarine : Brushes.SlateGray;
    public Brush RemoteBrush => remoteSpeaking && wasConnected ? Brushes.MediumAquamarine : Brushes.SlateGray;
    public RelayCommand RefreshNetworkCommand { get; }
    public RelayCommand CopyIpCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ToggleCommand { get; }
    public RelayCommand MuteCommand { get; }
    public RelayCommand RestoreCommand { get; }

    public MainViewModel(LogService log)
    {
        this.log = log;
        configuration = new(log);
        config = configuration.Load();
        Text.Language = config.Language;
        enabled = config.Enabled;
        if (Settings != null) Settings.PropertyChanged -= SettingsChanged;
        Settings = new(config);
        Settings.PropertyChanged += SettingsChanged;
        hasPendingChanges = false;
        microphone = new(log);
        automation = new(microphone);
        microphone.ExternalChange += () => dispatcher.BeginInvoke(new Action(() =>
        {
            if (disposed) return;
            enabled = false;
            automation.Stop();
            notice = "Mikrofon extern geändert. Automation wurde pausiert; bei Bedarf wieder aktivieren.";
            RefreshStatus();
        }));
        Text.PropertyChanged += LanguageChanged;
        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += NetworkAddressesChanged;
        RefreshNetworkCommand = new(RefreshNetwork);
        CopyIpCommand = new(() => Guard(() =>
        {
            if (SelectedLocalAddress == null) return;
            Clipboard.SetText(SelectedLocalAddress.Address);
            notice = Text["IpCopied"];
            Changed(nameof(Notice));
        }), () => SelectedLocalAddress != null);
        StartCommand = new(() => Guard(Start), () => !running);
        StopCommand = new(Stop, () => running || autoStart.Requested);
        SaveCommand = new(() => Guard(Save));
        RefreshCommand = new(() => Guard(RefreshDevices), () => !running);
        ToggleCommand = new(() => Guard(Toggle));
        MuteCommand = new(() => Guard(() => { microphone.ManualMute(); detector.Reset(); Publish("speaking"); RefreshStatus(); }), () => microphone.HasDevice);
        RestoreCommand = new(() => Guard(() => { enabled = false; automation.Stop(); notice = "Automation pausiert. Der vorherige Eingangspegel wurde wiederhergestellt."; RefreshStatus(); }), () => microphone.AttenuatedByRoomMute);
        timer = new(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromMilliseconds(40) };
        timer.Tick += (_, _) => Guard(Tick);
        retryTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromSeconds(2) };
        retryTimer.Tick += (_, _) => TryAutomaticStart();
        Guard(RefreshDevices);
        RefreshNetwork();
        if (configuration.LoadWarning != null) notice = configuration.LoadWarning;
    }

    private void SettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ShortcutLabel)) return;
        if (initialized && !running && e.PropertyName == nameof(SettingsViewModel.AudioDeviceId)) Guard(SelectIdleMicrophone);
        hasPendingChanges = true;
        Changed(nameof(PendingText));
        Changed(nameof(PendingBrush));
    }
    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UiText.Language)) { Settings.NotifyLanguageChanged(); Changed(""); }
    }
    private void NetworkAddressesChanged(object? sender, EventArgs e) =>
        dispatcher.BeginInvoke(new Action(() => { if (!disposed) RefreshNetwork(); }));
    private void RefreshNetwork()
    {
        try
        {
            string? previous = SelectedLocalAddress?.Address;
            var addresses = LocalNetworkService.GetAddresses(Settings.PartnerIp);
            LocalAddresses.Clear();
            foreach (var address in addresses) LocalAddresses.Add(address);
            SelectedLocalAddress = LocalAddresses.FirstOrDefault(a => a.Address == previous) ?? LocalAddresses.FirstOrDefault();
        }
        catch (Exception ex) { log.Write("Local adapter enumeration failed", ex); }
    }

    public void Initialize()
    {
        initialized = true;
        microphone.RecoverPending();
        Guard(SelectIdleMicrophone);
        InitializeUpdates();
        try { StartupService.Apply(config.StartWithWindows); }
        catch (Exception ex) { log.Write("Startup registration could not be refreshed", ex); }
        ConfigureShortcut();
        // Reuse the saved configuration; never replace an unavailable saved microphone with another device.
        if (AutoStartPolicy.IsConfigured(config))
        {
            autoStart.Request(Environment.TickCount64);
            TryAutomaticStart();
        }
    }

    private void RefreshDevices()
    {
        Devices.Clear();
        foreach (var item in AudioMonitorService.GetDevices()) Devices.Add(item);
        if (string.IsNullOrWhiteSpace(Settings.AudioDeviceId) && string.IsNullOrWhiteSpace(config.AudioDeviceId)) Settings.AudioDeviceId = Devices.FirstOrDefault()?.Id ?? "";
        Changed(nameof(Settings));
        notice = Devices.Count == 0 ? "Kein aktives Mikrofon gefunden. Windows-Mikrofonzugriff und Geräte prüfen." : "Mikrofon und Partner-IP einstellen, dann starten.";
        Changed(nameof(Notice));
    }
    private void Save()
    {
        AppConfig next;
        try
        {
            next = Settings.Build(enabled);
            StartupService.Apply(next.StartWithWindows);
            configuration.Save(next);
        }
        catch (Exception ex) { notice = ex.Message; Changed(nameof(Notice)); return; }
        bool restart = running || autoStart.Requested;
        StopRuntime();
        config = next;
        Settings.PropertyChanged -= SettingsChanged;
        Settings = new(config);
        Settings.PropertyChanged += SettingsChanged;
        hasPendingChanges = false;
        Changed(nameof(Settings));
        ConfigureShortcut();
        if (restart)
        {
            autoStart.Request(Environment.TickCount64);
            TryAutomaticStart();
            if (running) notice = "Einstellungen gespeichert und Verbindung neu gestartet.";
        }
        else notice = "Einstellungen gespeichert.";
        RefreshStatus();
    }
    private void Start()
    {
        var next = Settings.Build(enabled);
        if (next.Validate(false) is { } error) throw new InvalidDataException(error);
        if (string.IsNullOrWhiteSpace(next.AudioDeviceId)) throw new InvalidDataException("Bitte ein verfügbares Mikrofon auswählen.");
        StartupService.Apply(next.StartWithWindows);
        configuration.Save(next);
        config = next;
        hasPendingChanges = false;
        RefreshNetwork();
        ConfigureShortcut();
        autoStart.Request(Environment.TickCount64);
        TryAutomaticStart();
    }
    private void TryAutomaticStart()
    {
        if (disposed || running || !autoStart.IsDue(Environment.TickCount64)) return;
        try
        {
            var devices = AudioMonitorService.GetDevices();
            if (!devices.Any(d => d.Id == config.AudioDeviceId))
                throw new IOException(Text["AudioWait"]);
            // Add newly appeared devices without removing the user's selected device from the UI.
            foreach (var item in devices)
                if (!Devices.Any(d => d.Id == item.Id)) Devices.Add(item);
            StartRuntime();
            autoStart.Succeeded();
            retryTimer.Stop();
        }
        catch (Exception ex) { Fail(ex); }
    }
    public void SetShortcutRecording(bool recording)
    {
        if (disposed || !initialized) return;
        if (recording) pushToTalk?.Configure("None");
        else ConfigureShortcut();
    }
    private void ConfigureShortcut()
    {
        if (!initialized) return;
        try
        {
            if (pushToTalk == null)
            {
                pushToTalk = new GlobalPushToTalkService();
                pushToTalk.HeldChanged += held => Guard(() => SetOverride(held));
            }
            shortcutAvailable = pushToTalk.Configure(config.PushToTalkKey) && config.PushToTalkKey != "None";
            if (!shortcutAvailable && config.PushToTalkKey != "None")
                log.Write("Push-to-talk shortcut unavailable: " + config.PushToTalkKey);
        }
        catch (Exception ex) { shortcutAvailable = false; log.Write("Push-to-talk setup failed", ex); }
        Changed(nameof(ShortcutStatus));
    }
    private bool EffectiveSpeaking => !microphone.Muted && (overrideHeld || detector.Speaking);
    private long EffectiveStartedAt => overrideHeld
        ? detector.Speaking ? Math.Min(overrideStartedAt, detector.StartedAt) : overrideStartedAt
        : detector.StartedAt;
    private void SetOverride(bool held)
    {
        if (disposed || !running || !microphone.HasDevice) { overrideHeld = false; return; }
        if (held && microphone.Muted) return;
        if (overrideHeld == held) return;
        overrideHeld = held;
        overrideStartedAt = held ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : 0;
        log.Write(held ? "Push-to-talk override pressed" : "Push-to-talk override released");
        if (held) automation.Stop(); // Only owned ducking is restored; manual mute is never altered.
        Publish("speaking");
        Tick();
    }

    private void StartRuntime()
    {
        StopRuntime();
        int currentGeneration = ++generation;
        try
        {
            microphone.DuckPercent = config.DuckPercent;
            microphone.Select(config.AudioDeviceId);
            microphone.AllowAutomation();
            microphoneName = Devices.FirstOrDefault(d => d.Id == config.AudioDeviceId)?.Name ?? "Mikrofon";
            audioErrorHandler = error => dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation == currentGeneration && running) Fail(error);
            }));
            audio.Failed += audioErrorHandler;
            audio.Start(config.AudioDeviceId, config.NoiseSuppression);
            if (!string.IsNullOrWhiteSpace(config.PartnerIp))
            {
            var current = new NetworkService(config);
            network = current;
            current.Received += message => dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation != currentGeneration || !running) return;
                Guard(() =>
                {
                    if (current.Peer.Accept(message, Environment.TickCount64))
                    {
                        // Respond promptly during handshake; never echo ordinary states indefinitely.
                        if (message.Type == "hello") Publish("state");
                        Tick();
                    }
                });
            }));
            current.Failed += error => dispatcher.BeginInvoke(new Action(() =>
            {
                if (generation == currentGeneration && running) Fail(error);
            }));

            }
            running = true;
            detector.Reset();
            nextState = nextHello = 0;
            network?.Start();

            timer.Start();
            notice = network == null ? "Lokaler Pegeltest läuft. Für die Verbindung zum zweiten PC eine Partner-IP eintragen." : "Überwachung läuft. Auf beiden PCs denselben UDP-Port und jeweils die Partner-IP verwenden.";
            log.Write("RoomMute monitoring started");
            Tick();
            CommandManagerRefresh();
        }
        catch { StopRuntime(); throw; }
    }

    private void Tick()
    {
        if (!running) return;
        long now = Environment.TickCount64;
        var reading = audio.Latest;
        if (now - reading.ReceivedAt > 2000) throw new IOException("Das Mikrofon liefert keine Daten mehr. Gerät prüfen und neu starten.");
        level = reading.Decibels;
        actualMuted = microphone.Muted;
        bool changed;
        if (actualMuted)
        {
            changed = detector.Speaking;
            detector.Reset();
        }
        else changed = detector.Update(level, now, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), config, reading.SpeechProbability);
        if (changed)
        {
            log.Write(detector.Speaking ? "Local speaking started" : "Local speaking stopped");
            Publish("speaking");
        }
        if (network == null) { automation.Stop(); RefreshStatus(); return; }
        network.Peer.CheckTimeout(now, config.PartnerTimeout);
        bool connected = network.Peer.Connected;
        if (wasConnected != connected)
        {
            log.Write(connected ? "Partner connected: " + network.Peer.Current?.ClientName : "Partner disconnected");
            if (!connected) automation.Stop();
        }
        wasConnected = connected;
        var remote = network.Peer.Current;
        partnerName = connected ? remote?.ClientName ?? "Partner" : "Noch kein Partner";
        bool nextSpeaking = connected && remote?.Speaking == true;
        if (remoteSpeaking != nextSpeaking) log.Write(nextSpeaking ? "Remote speaking started" : "Remote speaking stopped");
        remoteSpeaking = nextSpeaking;
        var localState = new SpeakingState(EffectiveSpeaking, EffectiveStartedAt, config.Priority, config.ClientId);
        var remoteState = new SpeakingState(remoteSpeaking, remote?.StartedAt ?? 0, remote?.Priority ?? 0, remote?.ClientId ?? "");
        automation.Update(enabled, connected, localState, remoteState, config.FirstSpeakerWins, config.ReleaseDelay, now, overrideHeld);
        if (lastPriority != automation.PartnerHasPriority)
        {
            lastPriority = automation.PartnerHasPriority;
            log.Write(lastPriority ? "Remote has priority" : "Remote priority released");
        }
        if (now >= nextHello) { Publish("hello"); Publish("heartbeat"); nextHello = now + config.HeartbeatInterval; }
        else if (now >= nextState) Publish("state");
        if (now >= nextState) nextState = now + 500;
        RefreshStatus();
    }
    private void Publish(string type)
    {
        network?.Send(type, EffectiveSpeaking, EffectiveStartedAt, microphone.Muted);
    }
    private void Toggle()
    {
        enabled = !enabled;
        if (!enabled) automation.Stop();
        else microphone.AllowAutomation();
        config.Enabled = enabled;
        configuration.Save(config);
        notice = enabled ? "Automation aktiviert." : "Automation pausiert. Mikrofonzustand wiederhergestellt.";
        RefreshStatus();
    }
    public void Stop()
    {
        autoStart.Cancel();
        retryTimer.Stop();
        StopRuntime();
    }
    private void StopRuntime()
    {
        ++generation;
        timer?.Stop();
        running = false;
        overrideHeld = false; overrideStartedAt = 0;
        notice = "Überwachung gestoppt.";
        try { network?.Send("bye", false, 0, microphone.Muted); }
        catch (Exception ex) { log.Write("Could not send goodbye", ex); }
        try { network?.Dispose(); } catch (Exception ex) { log.Write("Network cleanup", ex); }
        network = null;
        if (audioErrorHandler != null) audio.Failed -= audioErrorHandler;
        audioErrorHandler = null;
        try { audio.Dispose(); } catch (Exception ex) { log.Write("Audio cleanup", ex); }
        try { automation.Stop(); }
        catch (Exception ex) { notice = "Mikrofon konnte nicht wiederhergestellt werden. Bitte in Windows prüfen."; log.Write(notice, ex); }
        detector.Reset();
        wasConnected = remoteSpeaking = false;
        level = -96;
        log.Write("Monitoring stopped; microphone restore requested");
        RefreshStatus();
        CommandManagerRefresh();
    }
    private void RefreshStatus()
    {
        bool previouslyOwned = ownedMute;
        try { actualMuted = microphone.Muted; ownedMute = microphone.AttenuatedByRoomMute; }
        catch (Exception ex) { log.Write("Microphone state unavailable", ex); }
        if (previouslyOwned != ownedMute) CommandManagerRefresh();
        Changed("");
    }
    private static void CommandManagerRefresh() => System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception ex) { Fail(ex); }
    }
    private void Fail(Exception error)
    {
        log.Write("Operation failed", error);
        StopRuntime();
        notice = error.Message + (microphone.AttenuatedByRoomMute ? " Mikrofon-Wiederherstellung fehlgeschlagen; bitte in Windows prüfen." : "");
        if (autoStart.Requested && !disposed)
        {
            autoStart.Failed(Environment.TickCount64);
            retryTimer.Start();
        }
        RefreshStatus();
    }
    public void EmergencyRestore()
    {
        enabled = false; // Prevent re-ducking during shutdown or error dialogs.

        try { microphone.Restore(); } catch (Exception ex) { log.Write("Emergency restore failed", ex); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        DisposeUpdates();
        autoStart.Cancel(); retryTimer.Stop();
        pushToTalk?.Dispose();
        Text.PropertyChanged -= LanguageChanged;
        Settings.PropertyChanged -= SettingsChanged;
        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= NetworkAddressesChanged;
        StopRuntime();
        try { microphone.Dispose(); } catch (Exception ex) { log.Write("Microphone cleanup failed", ex); }
    }
}
public sealed record LanguageChoice(string Code, string Label) { public override string ToString() => Label; }
