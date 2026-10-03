using System.ComponentModel;
using System.Globalization;
namespace RoomMute.Core;

/// <summary>Observable, shared UI strings. Language choice never affects the wire protocol.</summary>
public sealed class UiText : INotifyPropertyChanged
{
    public static UiText Current { get; } = new();
    private string language = "de";
    private static readonly IReadOnlyDictionary<string, (string De, string En)> Strings =
        new Dictionary<string, (string De, string En)>
    {
        ["Volume"] = ("Mikrofonlautstärke", "Microphone volume"),
        ["VolumeTip"] = ("Aktueller Windows-Eingangspegel. Änderungen gelten sofort und pausieren die Automation. Während Ducking zeigt der Regler den tatsächlich abgesenkten Pegel. Der manuelle Mute-Schalter bleibt erhalten.", "Current Windows input volume. Changes apply immediately and pause automation. During ducking the slider shows the actual lowered input. Manual mute stays enabled."),
        ["VolumeManual"] = ("Mikrofonpegel geändert. Automation pausiert.", "Microphone volume changed. Automation paused."),
        ["Updates"] = ("Updates", "Updates"),
        ["UpdateIdle"] = ("Noch nicht geprüft.", "Not checked yet."),
        ["UpdateChecking"] = ("Suche nach Updates …", "Checking for updates …"),
        ["UpdateAvailable"] = ("Neue Version verfügbar", "New version available"),
        ["UpdateCurrent"] = ("Du hast die aktuelle Version.", "You are up to date."),
        ["UpdateFailed"] = ("Update konnte nicht abgeschlossen werden. Erneut versuchen.", "Could not complete the update. Try again."),
        ["UpdateNoNotes"] = ("Nach einer Prüfung erscheinen hier die Patchnotes der neuesten Version.", "Check for updates to see the latest release notes here."),
        ["UpdateCheck"] = ("Jetzt prüfen", "Check now"),
        ["UpdateDownload"] = ("Update herunterladen", "Download update"),
        ["UpdateInstall"] = ("Neue Version starten", "Start new version"),
        ["UpdateFolder"] = ("Downloadordner", "Download folder"),
        ["UpdateDownloading"] = ("Update wird heruntergeladen und geprüft …", "Downloading and verifying update …"),
        ["UpdateInstalling"] = ("Neue Version wird vorbereitet …", "Preparing the new version …"),
        ["UpdateReady"] = ("Download geprüft. Mit „Neue Version starten“ wechseln.", "Download verified. Choose “Start new version” to switch."),
        ["UpdateApplyFirst"] = ("Bitte zuerst die offenen Einstellungsänderungen übernehmen.", "Apply your pending settings changes first."),
        ["UpdateRestoreFailed"] = ("Mikrofon konnte nicht wiederhergestellt werden. Updatewechsel abgebrochen.", "Could not restore microphone input. Update switch canceled."),
        ["UpdateVersions"] = ("Installiert: {0} · Neueste Version: {1}", "Installed: {0} · Latest: {1}"),
        ["UpdatePrivacy"] = ("Prüfung bei Start und alle 6 Stunden über GitHub. Keine Mikrofon- oder Audiodaten werden gesendet. Patchnotes erscheinen in der Sprache des Releases.", "Checks GitHub at startup and every 6 hours. No microphone or audio data is sent. Release notes use the release's language."),
        ["RecordShortcut"] = ("Aufnehmen", "Record"),
        ["RecordTitle"] = ("Shortcut aufnehmen", "Record shortcut"),
        ["RecordHint"] = ("Drücke eine Taste/Kombination (z. B. Strg + 5) oder Mouse3 (mittlere Maustaste). Esc bricht ab. Nur Modifikatortasten und F12 können nicht aufgenommen werden.", "Press a key/combination (e.g. Ctrl + 5) or Mouse3 (middle mouse button). Esc cancels. Modifier-only keys and F12 cannot be recorded."),
        ["RecordWaiting"] = ("Warte auf Tastendruck …", "Waiting for a key …"),
        ["RecordCancel"] = ("Abbrechen", "Cancel"),
        ["RecordClear"] = ("Shortcut deaktivieren", "Disable shortcut"),
        ["PushAnyway"] = ("Trotzdem sprechen", "Push to talk anyway"),
        ["PushTip"] = ("Taste halten: RoomMute hebt nur die eigene Absenkung auf und meldet Sprechbereitschaft. Loslassen: normale Koordination. Ein manuelles Mute bleibt erhalten. Funktioniert auch im Tray. Mit Aufnehmen eine einzelne Taste oder Kombination festlegen, dann Übernehmen. Einzelne Tasten werden während RoomMute läuft global belegt.", "Hold the key: RoomMute restores only its own input reduction and announces speaking. Release: normal coordination resumes. Manual mute is preserved. Works in the tray. Use Record to choose a single key or combination, then Apply changes. Single keys are reserved globally while RoomMute is running."),
        ["PushActive"] = ("PUSH TO TALK · FREIGABE AKTIV", "PUSH TO TALK · OVERRIDE ACTIVE"),
        ["PushHint"] = ("Taste halten, um mit dem ursprünglichen Eingangspegel zu sprechen. Loslassen beendet die Freigabe.", "Hold the key to speak at your original input level. Release to end the override."),
        ["PushReady"] = ("{0} halten", "Hold {0}"),
        ["PushOff"] = ("Shortcut aus", "Shortcut off"),
        ["PushUnavailable"] = ("{0} belegt oder nicht verfügbar", "{0} busy or unavailable"),
        ["PushInvalid"] = ("Ungültiger Push-to-talk-Shortcut.", "Invalid push-to-talk shortcut."),
        ["AutoWaiting"] = ("Automatischer Start wird wiederholt …", "Retrying automatic startup …"),
        ["AutoRetry"] = ("Nächster Startversuch erfolgt automatisch.", "The next startup attempt is automatic."),
        ["AudioWait"] = ("Warte auf das gespeicherte Mikrofon.", "Waiting for the saved microphone."),
        ["StartupTip"] = ("Nach der Windows-Anmeldung startet RoomMute mit der gespeicherten Partner-IP automatisch. Ein später gestarteter Partner wird erkannt. Wenn Gerät oder Netzwerk noch fehlen, wird erneut versucht. Stop beendet die automatischen Versuche für diese Sitzung.", "After Windows sign-in, RoomMute starts automatically with the saved partner IP. A partner started later is detected. Missing devices or network trigger retries. Stop cancels automatic attempts for this session."),
        ["Subtitle"] = ("Sprachkoordination im selben Raum", "Voice coordination in the same room"),
        ["Start"] = ("Starten", "Start"),
        ["Stop"] = ("Stop", "Stop"),
        ["Apply"] = ("Änderungen übernehmen", "Apply changes"),
        ["ApplyHint"] = ("Regler und Felder mit „Übernehmen“ aktivieren. Die Sprachwahl gilt sofort.", "Apply slider and field changes with “Apply changes”. Language changes immediately."),
        ["Applied"] = ("Alles übernommen", "All changes applied"),
        ["Pending"] = ("Nicht übernommene Änderungen", "Unapplied changes"),
        ["Local"] = ("DEIN MIKROFON", "YOUR MICROPHONE"),
        ["Partner"] = ("PARTNER & VERBINDUNG", "PARTNER & CONNECTION"),
        ["Speak"] = ("Sprech-Schwelle", "Speak threshold"),
        ["Silence"] = ("Ruhe-Schwelle", "Silence threshold"),
        ["SpeakTip"] = ("Oberhalb dieser Schwelle beginnt die Sprecherkennung, sobald die Attack-Zeit abgelaufen ist.", "Above this threshold, speaking starts once the attack time has elapsed."),
        ["SilenceTip"] = ("Unterhalb dieser Schwelle endet die Sprecherkennung nach der Release-Zeit. Sie muss niedriger als die Sprech-Schwelle sein, damit die Erkennung nicht flattert.", "Below this threshold, speaking ends after the release time. Keep it below the speak threshold to prevent rapid switching."),
        ["Attack"] = ("Attack (ms)", "Attack (ms)"),
        ["Release"] = ("Release (ms)", "Release (ms)"),
        ["AttackTip"] = ("So lange muss dein Pegel durchgehend oberhalb der Sprech-Schwelle liegen, bevor du als sprechend giltst. Kurze Geräusche werden dadurch ignoriert.", "Your level must stay above the speak threshold for this long before you count as speaking. This filters out brief noises."),
        ["ReleaseTip"] = ("So lange muss dein Pegel unterhalb der Ruhe-Schwelle bleiben, bevor du als ruhig giltst. Danach wartet der Partner zusätzlich diese Zeit bis zur Pegel-Wiederherstellung. 350 ms können insgesamt etwa 700 ms ergeben.", "Your level must remain below the silence threshold for this long before you count as silent. The partner then waits this additional time before restoring its input volume. 350 ms can mean about 700 ms in total."),
        ["Duck"] = ("Ducking-Restpegel", "Ducking remaining level"),
        ["DuckTip"] = ("Anteil des bisherigen Windows-Eingangspegels, wenn dein Partner Vorrang hat. 20 % von vorher 80 % ergibt 16 %. 100 % bedeutet keine Absenkung. Betrifft alle Apps, die dieses Mikrofon verwenden.", "Fraction of the original Windows input setting while your partner has priority. 20% of an original 80% gives 16%. 100% means no reduction. Affects all apps using this microphone."),
        ["Mute"] = ("Manuell muten", "Mute manually"),
        ["Restore"] = ("Pegel wiederherstellen", "Restore input level"),
        ["OwnIp"] = ("IP dieses PCs", "This PC’s IP"),
        ["Copy"] = ("Kopieren", "Copy"),
        ["IpTip"] = ("Diese IPv4-Adresse auf dem anderen PC als Partner-IP eintragen. Bei mehreren Netzwerkadaptern das gemeinsame LAN wählen. Die Liste zeigt lokale Adapter; es wird kein Internetdienst abgefragt.", "Enter this IPv4 address as the partner IP on the other PC. With multiple adapters, select the shared LAN. Addresses come from local adapters; no internet service is queried."),
        ["Refresh"] = ("Aktualisieren", "Refresh"),
        ["RefreshDevices"] = ("Mikrofone aktualisieren (Überwachung vorher stoppen)", "Refresh microphones (stop monitoring first)"),
        ["PcName"] = ("Name dieses PCs", "This PC’s name"),
        ["PartnerIp"] = ("Partner-IP", "Partner IP"),
        ["Port"] = ("UDP-Port", "UDP port"),
        ["Priority"] = ("Priorität", "Priority"),
        ["PriorityTip"] = ("Niedrigere Zahl gewinnt bei Sprechbeginn innerhalb von 50 ms. Bei Gleichstand entscheidet die Client-ID. Windows-Uhren sollten synchron sein.", "The lower number wins when speaking starts within 50 ms. Equal values are resolved by client ID. Keep Windows clocks synchronized."),
        ["FirstSpeaker"] = ("Erster Sprecher gewinnt", "First speaker wins"),
        ["FirstTip"] = ("Wer früher spricht, behält seinen Eingangspegel. Der andere PC wird abgesenkt. Ohne diese Option erhält der Partner immer Vorrang.", "Whoever starts speaking first keeps their input level. The other PC is ducked. Without this option, the partner always takes priority."),
        ["Startup"] = ("Mit Windows starten", "Start with Windows"),
        ["Tray"] = ("In den Tray minimieren", "Minimize to tray"),
        ["Minimized"] = ("Minimiert starten", "Start minimized"),
        ["Privacy"] = ("Nur Statusdaten im LAN · keine Audioaufzeichnung", "LAN status only · no audio recording"),
        ["Language"] = ("Sprache / Language", "Sprache / Language"),
        ["NoAddress"] = ("Keine LAN-IPv4 verfügbar", "No LAN IPv4 available"),
        ["NoPartner"] = ("Noch kein Partner", "No partner yet"),
        ["NoMicrophone"] = ("Kein Mikrofon ausgewählt", "No microphone selected"),
        ["Ready"] = ("BEREIT", "READY"),
        ["Speaking"] = ("SPRICHT", "SPEAKING"),
        ["Silent"] = ("RUHIG", "SILENT"),
        ["Muted"] = ("STUMM", "MUTED"),
        ["Offline"] = ("OFFLINE", "OFFLINE"),
        ["SelectMic"] = ("MIKROFON WÄHLEN", "SELECT MICROPHONE"),
        ["Ducking"] = ("DUCKING · {0:0} % RESTPEGEL", "DUCKING · {0:0}% REMAINING"),
        ["ManualMute"] = ("MANUELL STUMM", "MANUALLY MUTED"),
        ["Active"] = ("MIKROFON AKTIV", "MICROPHONE ACTIVE"),
        ["SelectHint"] = ("Mikrofon wählen und starten. Ohne Partner-IP kannst du den Pegel lokal testen.", "Choose a microphone and start. Leave the partner IP empty to test the level locally."),
        ["DuckHint"] = ("Dein Eingangspegel ist abgesenkt und wird danach wiederhergestellt.", "Your input level is lowered and will be restored afterwards."),
        ["MuteHint"] = ("Manuelles Mute bleibt erhalten. In Windows oder am Mikrofon aufheben.", "Manual mute is preserved. Unmute in Windows or on your microphone."),
        ["ActiveHint"] = ("Dein Mikrofon ist offen. Sprich ganz natürlich.", "Your microphone is open. Speak naturally."),
        ["ReadyHint"] = ("Bereit. Auf beiden PCs Mikrofon und Partner-IP einrichten.", "Ready. Set up the microphone and partner IP on both PCs."),
        ["Waiting"] = ("Warte auf Partner …", "Waiting for partner …"),
        ["Disconnected"] = ("Nicht verbunden", "Not connected"),
        ["Connected"] = ("Verbunden mit {0}", "Connected to {0}"),
        ["LocalTest"] = ("Lokaler Pegeltest", "Local level test"),
        ["LocalOnly"] = ("Lokales Netzwerk · keine Cloud", "Local network · no cloud"),
        ["WaitingPriority"] = ("WARTET AUF VERBINDUNG", "WAITING FOR CONNECTION"),
        ["PartnerPriority"] = ("PARTNER HAT VORRANG", "PARTNER HAS PRIORITY"),
        ["YouPriority"] = ("DU HAST VORRANG", "YOU HAVE PRIORITY"),
        ["NextTurn"] = ("BEREIT FÜR DAS NÄCHSTE WORT", "READY FOR THE NEXT TURN"),
        ["PartnerFirst"] = ("Partner hat Vorrang", "Partner takes priority"),
        ["AutomationOn"] = ("Automation aktiv", "Automation enabled"),
        ["AutomationOff"] = ("Automation pausiert", "Automation paused"),
        ["Enable"] = ("Automation aktivieren", "Enable automation"),
        ["Pause"] = ("Automation pausieren", "Pause automation"),
        ["Open"] = ("RoomMute öffnen", "Open RoomMute"),
        ["Exit"] = ("Beenden", "Exit"),
        ["IpCopied"] = ("IP-Adresse kopiert.", "IP address copied."),
        ["InvalidNumber"] = ("{0}: Bitte eine ganze Zahl eingeben.", "{0}: Enter a whole number."),
        ["AlreadyRunning"] = ("RoomMute läuft bereits. Öffne die Anwendung über das Tray-Symbol.", "RoomMute is already running. Open it from the system tray."),
        ["Fatal"] = ("RoomMute wurde wegen eines Fehlers beendet. Bitte Mikrofonstatus prüfen.", "RoomMute closed due to an error. Please check your microphone state."),
        ["StartupFailed"] = ("RoomMute konnte nicht starten.", "RoomMute could not start."),
        ["RestoreFailed"] = ("Mikrofon-Wiederherstellung fehlgeschlagen; bitte in Windows prüfen.", "Microphone restoration failed; please check Windows."),
        ["Message1"] = ("Mikrofon wählen und die IP des Partner-PCs eintragen.", "Choose your microphone and enter the partner PC’s IP address."),
        ["Message2"] = ("Mikrofon extern geändert. Automation wurde pausiert; bei Bedarf wieder aktivieren.", "Microphone changed externally. Automation paused; enable it again when needed."),
        ["Message3"] = ("Automation pausiert. Der vorherige Eingangspegel wurde wiederhergestellt.", "Automation paused. The previous input level has been restored."),
        ["Message4"] = ("Kein aktives Mikrofon gefunden. Windows-Mikrofonzugriff und Geräte prüfen.", "No active microphone found. Check devices and Windows microphone permissions."),
        ["Message5"] = ("Mikrofon und Partner-IP einstellen, dann starten.", "Set your microphone and partner IP, then start."),
        ["Message6"] = ("Einstellungen gespeichert und Verbindung neu gestartet.", "Settings saved and connection restarted."),
        ["Message7"] = ("Einstellungen gespeichert.", "Settings saved."),
        ["Message8"] = ("Bitte ein verfügbares Mikrofon auswählen.", "Please choose an available microphone."),
        ["Message9"] = ("Lokaler Pegeltest läuft. Für die Verbindung zum zweiten PC eine Partner-IP eintragen.", "Local level test running. Enter a partner IP to connect to the second PC."),
        ["Message10"] = ("Überwachung läuft. Auf beiden PCs denselben UDP-Port und jeweils die Partner-IP verwenden.", "Monitoring active. Use the same UDP port and each other’s IP on both PCs."),
        ["Message11"] = ("Das Mikrofon liefert keine Daten mehr. Gerät prüfen und neu starten.", "The microphone is no longer providing data. Check the device and restart."),
        ["Message12"] = ("Automation aktiviert.", "Automation enabled."),
        ["Message13"] = ("Automation pausiert. Mikrofonzustand wiederhergestellt.", "Automation paused. Microphone state restored."),
        ["Message14"] = ("Überwachung gestoppt.", "Monitoring stopped."),
        ["Message15"] = ("Mikrofon konnte nicht wiederhergestellt werden. Bitte in Windows prüfen.", "Could not restore the microphone. Please check Windows."),
        ["Message16"] = ("Konfiguration konnte nicht geladen werden. Standardwerte sind aktiv; bitte Einstellungen prüfen.", "Could not load configuration. Defaults are active; please check your settings."),
        ["Message17"] = ("Der PC-Name muss 1–64 Zeichen enthalten.", "The PC name must contain 1–64 characters."),
        ["Message18"] = ("Die Client-ID ist ungültig.", "The client ID is invalid."),
        ["Message19"] = ("Bitte eine gültige IPv4-Adresse des Partner-PCs eingeben.", "Enter a valid IPv4 address for the partner PC."),
        ["Message20"] = ("Der Port muss zwischen 1024 und 65535 liegen.", "The port must be between 1024 and 65535."),
        ["Message21"] = ("Die Priorität muss zwischen 0 und 10000 liegen.", "Priority must be between 0 and 10000."),
        ["Message22"] = ("Die Ruhe-Schwelle muss unter der Sprech-Schwelle liegen.", "The silence threshold must be lower than the speak threshold."),
        ["Message23"] = ("Ducking-Restpegel muss zwischen 1 und 100 % liegen.", "The remaining ducking level must be between 1 and 100%."),
        ["Message24"] = ("Attack: 0–2000 ms; Release: 50–5000 ms.", "Attack: 0–2000 ms; release: 50–5000 ms."),
        ["Message25"] = ("Ungültige Heartbeat-/Timeout-Einstellungen.", "Invalid heartbeat or timeout settings."),
        ["Message26"] = ("Ungültige Sprache.", "Invalid language."),
        ["Message27"] = ("Autostart bitte aus RoomMute.exe konfigurieren.", "Please configure startup from RoomMute.exe."),
        ["Message28"] = ("Programmpfad fehlt.", "Application path unavailable."),
        ["Message29"] = ("Leere Konfiguration.", "Empty configuration."),
    };
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Language
    {
        get => language;
        set
        {
            if (value is not ("de" or "en")) throw new ArgumentOutOfRangeException(nameof(value));
            if (language == value) return;
            language = value;
            PropertyChanged?.Invoke(this, new(nameof(Language)));
            PropertyChanged?.Invoke(this, new("Item[]"));
        }
    }
    public CultureInfo Culture => CultureInfo.GetCultureInfo(language == "de" ? "de-DE" : "en-GB");
    public static IEnumerable<string> Keys => Strings.Keys;
    public string this[string key] => Strings.TryGetValue(key, out var entry)
        ? language == "de" ? entry.De : entry.En
        : throw new KeyNotFoundException("Missing UI text: " + key);
    public string Format(string key, params object[] values) => string.Format(Culture, this[key], values);
    public string Translate(string message)
    {
        foreach (var entry in Strings.Values)
            if (message == entry.De || message == entry.En) return language == "de" ? entry.De : entry.En;
        return message; // Windows/driver error details retain their system-provided wording.
    }
}




