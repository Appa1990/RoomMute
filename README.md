# RoomMute 1.7.0

Local voice coordination for people sharing the same room.

[Download for Windows](https://github.com/Appa1990/RoomMute/releases/latest) · [Homepage](https://lmnt-gaming.net/roommute.php) · [Issues](https://github.com/Appa1990/RoomMute/issues) · [Contributing](CONTRIBUTING.md)

RoomMute is open source under the [MIT license](LICENSE). NAudio, RNNoise, Silero and ONNX Runtime licenses are included in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

Windows-Desktop-Anwendung (.NET 8 / WPF) für zwei PCs im selben Raum. Der lokale Mikrofonpegel steuert die Sprechkoordination. Über UDP werden ausschließlich Statusdaten gesendet; keine Audiodaten werden gespeichert, übertragen oder transkribiert.

## Neu in 1.7.0

- **Strengerer Sprachfilter:** Zusätzlich zu RNNoise prüft Silero VAD das Originalsignal. Beide Modelle müssen Sprache bestätigen; Silero verlangt zwei aufeinanderfolgende 32-ms-Fenster. Der neue Modus Strenger ist voreingestellt, auch bei vorhandenen Einstellungen ohne Modus. Standard verwendet weiterhin RNNoise allein. Modus/Sprachsicherheit mit Übernehmen aktivieren.
- **Jeder Audioabschnitt zählt:** Alle 10-ms-Ergebnisse werden chronologisch ausgewertet. Die 40-ms-UI-Aktualisierung und Netzwerkpakete dürfen nicht mehr durch wiederholtes Verwenden einzelner Spitzen einen Sprechbeginn erzeugen. Eine begrenzte Warteschlange stoppt bei Überlast die Überwachung kontrolliert.
- **Live-KI-Anzeige:** Neben dem lokalen Status steht die aktuelle gemeinsame Sprachbewertung. Sie hilft, Geräusche, Schwellen und zu strenge Einstellungen zu unterscheiden. Modellwerte sind keine kalibrierten Wahrscheinlichkeiten.
- Weiterhin lokale CPU-Verarbeitung mit eingebettetem Modell; kein virtuelles Mikrofon oder Audio-Upload. RNNoise-DLL, ONNX-Laufzeit und Models-Ordner vollständig entpacken. CPU-/Speicherbedarf und Paketgröße steigen durch die zusätzliche Prüfung. Bei zu spät erkannter leiser/kurzer Sprache Sprachsicherheit verringern oder Standard auswählen. Sprechbeginn wird um die zusätzlichen Modellfenster verzögert; der Ton in Discord/Teamspeak wird nicht verzögert oder verarbeitet.
- Eine privat bereitgestellte Vape-Probe wurde lokal bei originalem und 2-/4-/8-fachem Eingangspegel geprüft: Strenger erzeugt keine Sprechstarts bei -35/-42 oder -25/-32 dBFS und 60 % Sprachsicherheit. Maximale gemeinsame Bewertung 3,7–7,2 %. RNNoise allein erreicht zeitweise 100 % und erzeugt bei höheren Pegeln Fehlstarts. Diese einzelne Probe beweist keine universelle Unterdrückung. Erzeugte Sprachsätze bleiben erkennbar. Die Aufnahme wird nicht veröffentlicht.

## Neu in 1.6.0

- **Lokaler KI-Geräuschfilter für die Erkennung:** RNNoise 0.2 reduziert Rauschen und bewertet, ob Sprache enthalten ist. Standardmäßig eingeschaltet; im Mikrofonbereich über KI-Geräuschfilter deaktivierbar. Änderungen mit Übernehmen aktivieren. Nur der interne Erkennungspfad wird verarbeitet. Discord/Teamspeak erhalten weiterhin das Originalmikrofon mit ihren eigenen Filtern; RoomMute installiert kein virtuelles Gerät.
- **Sprachsicherheit:** 20–95 %, Standard 60 %. Höhere Werte verlangen stärkere Hinweise auf Sprache und können Fehlstarts durch Atem-/Vape-Geräusche reduzieren; leise Sprache kann später erkannt werden. Während eines bestehenden Sprechturns gilt eine um 20 Prozentpunkte niedrigere Grenze, mindestens 10 %, damit kurze Pausen/Frikative nicht sofort freigeben. Lautstärkeschwellen sowie Attack und Release gelten weiterhin.
- Bei aktivem Filter zeigt die Mikrofonbar den gefilterten dBFS-Pegel. Nach dem Upgrade die Schwellen erneut prüfen. Ausschalten bringt die bisherige reine Pegelerkennung zurück. Der gehaltene Trotzdem-sprechen-Shortcut bleibt ein bewusster Override; manuelles Mute bleibt erhalten.
- Verarbeitung lokal auf der CPU mit eingebettetem Modell, ohne Cloud, GPU-Pflicht, Audioaufzeichnung oder Audioübertragung. Paket vollständig entpacken, einschließlich rnnoise.dll. Ein fehlender/unpassender Filter stoppt den Start mit einer Meldung, statt still ungeschützt weiterzulaufen; Filter kann ausgeschaltet werden.
- Der Filter unterscheidet Sprache von Geräuschen, aber nicht dich von anderen sprechenden Menschen. Vape-Geräusche können abhängig vom Mikrofon weiterhin durchkommen; keine garantierte Vape-Erkennung. Ein realer Test mit deinem Mikrofon bleibt erforderlich.

## Neu in 1.5.0

- **Updates im Tool:** Versionsanzeige und Hinweis auf neue stabile GitHub-Releases, Prüfung beim Start und alle 6 Stunden sowie manuell. Der Update-Dialog zeigt die Patchnotes des Releases, lädt das passende Windows-Paket herunter und prüft Dateigröße und SHA-256 gegen die GitHub-Release-Metadaten. Internet ist nur für diese Funktion nötig; Audioüberwachung und LAN-Verbindung laufen auch bei fehlgeschlagener Updateprüfung weiter.
- **Neue Version starten:** Das geprüfte ZIP wird in einen neuen Ordner unter `%LocalAppData%\RoomMute\Updates` entpackt. RoomMute stellt zuerst den Mikrofonpegel wieder her und beendet die alte Instanz. Die neue Instanz wartet auf deren Ende, übernimmt die gespeicherten Einstellungen und aktualisiert ihren Autostartpfad. Offene Einstellungsänderungen zuerst übernehmen. Der bisherige Programmordner bleibt erhalten; das neue Programm läuft aus dem Updateordner. Kein stilles Update im Hintergrund.
- **Mikrofonlautstärke:** Aktueller Windows-Eingangspegel in Prozent direkt neben Ducking. Der Regler verändert ihn sofort und pausiert die Automation; mit Automation aktivieren wieder fortsetzen. Während Ducking wird der tatsächliche abgesenkte Eingangspegel angezeigt. Der Regler ist unabhängig vom gemessenen dBFS-Signal und vom relativen Ducking-Restpegel.
- **Wiederherstellung nach Neustart:** Vor jeder automatischen Absenkung werden Geräte-ID, ursprünglicher Pegel und angewendeter Wert dauerhaft unter `%LocalAppData%\RoomMute\VolumeRecovery` gespeichert. Nach normalem Restore wird der Eintrag entfernt. Beim nächsten Start beziehungsweise bei erneuter Geräteauswahl wird eine verbliebene eigene Absenkung zurückgenommen, sofern der aktuelle Pegel noch zum gespeicherten angewendeten Wert passt. Externe Pegeländerungen und manuelles Mute bleiben erhalten; nicht erreichbare Geräte werden später erneut geprüft.
- Beim Windows-Sitzungsende wird die Automation gestoppt und der Pegel vor dem Exit wiederhergestellt. Fehlerdialoge verhindern erneutes Ducking während einer Wiederherstellung.

Ältere Versionen haben den ursprünglichen Pegel nicht dauerhaft gespeichert. Einen bereits vor dem Upgrade abgesenkten Eingang einmal am neuen Mikrofonregler auf deinen gewünschten Wert setzen. Bei Stromausfall, fehlendem Gerät oder beschädigter Wiederherstellungsdatei lässt sich ein Restore nicht immer garantieren; im Zweifel den angezeigten Windows-Pegel prüfen.

Updateprüfungen rufen ausschließlich das öffentliche GitHub-Repository auf, ohne Konto oder Token. Sie übertragen keine Mikrofon-, Audio- oder Partnerdaten. GitHub erhält wie bei einem normalen Abruf Netzwerk-/HTTP-Metadaten. Patchnotes bleiben in der Sprache des Releases. Downloads im Tool laufen über GitHub und werden separat von den Homepage-Downloads gezählt.

## Neu in 1.4.1

**Mouse3 (mittlere Maustaste)** lässt sich wie eine Tastaturtaste aufnehmen, auch mit Modifikatoren wie **Strg + Mouse3**. Aufnehmen klicken, das Mausrad drücken und Änderungen übernehmen. Halten hebt die eigene Absenkung auf; Loslassen stellt die normale Koordination wieder her. Die Erkennung läuft auch im Tray und im Hintergrund über Windows-Raw-Input. Bewegung und Scrollen aktivieren den Shortcut nicht; der Klick bleibt für die andere Anwendung verfügbar. Es läuft keine dauernde Maustastenabfrage, die 30-ms-Prüfung ist nur während des Haltens aktiv.

## Neu in 1.4

Shortcut-Aufnahme wie im Voice-Chat: **Aufnehmen** anklicken, Taste oder Kombination drücken und **Änderungen übernehmen**. Unterstützt Strg, Alt, Umschalt, Windows-Taste sowie Buchstaben, Zahlen, Funktionstasten, Navigation und Nummernblock. Nur Modifikatortasten und F12 werden nicht aufgenommen; Esc ist zum Abbrechen reserviert. Eine bereits durch Windows oder eine andere Anwendung belegte Kombination wird als nicht verfügbar angezeigt.

Beim Aufnehmen wird der alte globale Shortcut vorübergehend freigegeben. Abbrechen stellt ihn wieder her; ein neuer Shortcut bleibt zunächst ein Entwurf bis zum Übernehmen. Bei Kombinationen endet „Trotzdem sprechen“, sobald die Haupttaste oder einer der erforderlichen Modifikatoren losgelassen wird. Einzelne Tasten sind während RoomMute läuft global belegt; für eine Taste, die du auch zum Schreiben brauchst, verwende eine Kombination.

## Automatischer Betrieb seit 1.3

- **Push to talk anyway / Trotzdem sprechen:** F8 halten hebt RoomMutes eigene Absenkung sofort auf. Beim Loslassen gilt wieder die normale Koordination. Ein manuelles Mute bleibt erhalten. Die Taste meldet den eigenen PC als sprechend, zwingt den Partner aber nicht zur Absenkung; beide können bewusst gleichzeitig sprechen.
- Globaler Shortcut auch im Tray und bei einer anderen aktiven Anwendung. Im Dashboard **Aufnehmen** klicken und eine einzelne Taste oder Kombination drücken, z. B. **Strg + 5**, **Alt + A** oder **5**. **Esc** bricht die Aufnahme ab; **Shortcut deaktivieren** schaltet ihn aus. Mit **Änderungen übernehmen** aktivieren. Ist die Taste bereits belegt, zeigt RoomMute das an; eine andere wählen.
- Der Shortcut verwendet Windows-Hotkey-Nachrichten. Im Leerlauf gibt es keine Tastenabfrage; nur beim Halten wird alle 30 ms das Loslassen geprüft. Es werden keine Tastatureingaben aufgezeichnet. [Windows RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey).
- Sobald Mikrofon und Partner-IP gespeichert sind, startet RoomMute beim Öffnen die Überwachung und Verbindung automatisch. Mit **Mit Windows starten** und optional **Minimiert starten** läuft es nach der Anmeldung automatisch im Hintergrund.
- Ein später gestarteter Partner wird automatisch erkannt. Bei noch fehlendem gespeicherten Mikrofon oder Startfehlern wird nach 2, 5, 10 und anschließend jeweils 30 Sekunden erneut versucht; die Zeitprüfung kann bis zu 2 Sekunden hinzufügen. Bei laufender Verbindung werden regelmäßig Hello-Pakete gesendet. **Stop** beendet auch ausstehende automatische Starts.
- Ein gespeichertes pausiertes Automationsverhalten bleibt erhalten. Zum automatischen Ducking muss die Automation aktiv sein. Es wird kein anderes Mikrofon stillschweigend anstelle eines fehlenden gespeicherten Geräts ausgewählt.

## Dashboard seit 1.2

- Ein gemeinsames Dashboard ersetzt die separate Einstellungsseite; alle Bedienelemente sind ab 1040 × 740 WPF-Fenstermaßen ohne Scrollen sichtbar.
- Sprech-Schwelle (grün) und Ruhe-Schwelle (blau) sind direkt in der Mikrofonbar markiert und unmittelbar darunter einstellbar.
- Attack, Release, Ducking und Priorität haben erklärende Tooltips. Die Fragezeichen neben Attack/Release erläutern auch die zusätzliche Freigabeverzögerung.
- Eigene IPv4-Adresse mit Adapterauswahl und Kopierknopf. Die Auswahl ändert nicht die Netzwerkkonfiguration; sie hilft, die richtige Adresse auf dem Partner-PC einzutragen. Die Auflistung fragt keinen Internetdienst ab.
- Sprache oben rechts sofort zwischen Deutsch und Englisch umschalten. Dashboard, Statusmeldungen, Tooltips, Tray und eigene Validierungstexte wechseln mit; die Sprachwahl wird gespeichert. Gerätenamen und Fehlerdetails von Windows bleiben im Original.
- Regler und Felder werden über **Änderungen übernehmen / Apply changes** aktiviert. Die Pegelmarkierungen zeigen bereits die eingestellten Entwurfswerte; der Fußbereich weist auf noch nicht übernommene Änderungen hin. Das Übernehmen startet eine laufende Überwachung neu. Ein Sprachwechsel unterbricht sie nicht.


## Ducking seit 1.1

- Dunkler Hintergrund auch im äußeren Fensterbereich; der weiße Rand ist korrigiert.
- Automatisches **Ducking statt Mute**: RoomMute senkt den Windows-Eingangspegel vorübergehend ab.
- Im **Dashboard → Ducking-Restpegel**: Schieberegler von **1 bis 100 %**, Standard **20 %**.
- Die Prozentangabe bezieht sich auf den vorherigen Windows-Eingangsregler: 80 % ursprünglicher Pegel × 20 % Restpegel = 16 %. Sie beschreibt keine garantierte akustische Lautstärke oder dB-Dämpfung.
- 100 % bedeutet keine Absenkung. Änderungen mit **Änderungen übernehmen** übernehmen.
- Nach dem Sprechen, bei Stop, Pause, Timeout und regulärem Exit wird der vorherige Eingangspegel wiederhergestellt.
- Ein manuelles Mute bleibt erhalten. Der manuelle Stummschaltknopf ist weiterhin verfügbar.
- Externe Änderungen pausieren die Automation. Ein extern neu gewählter Eingangspegel wird nicht überschrieben; eine reine externe Mute-Änderung lässt den ursprünglichen Pegel wiederherstellen, ohne das manuelle Mute aufzuheben.

## Auf beiden PCs einrichten

1. Alte RoomMute-Version über das Tray-Menü **Beenden** schließen, damit sie ein eventuell gesetztes Mute aufheben kann.
2. Das neue portable ZIP auf beiden PCs lokal in einen eigenen Ordner entpacken und `RoomMute.exe` starten. Alle Paketdateien zusammen belassen. Die .NET-Laufzeit ist enthalten.
3. Das Aufnahmegerät wählen, das auch Discord/Teams usw. verwendet.
4. Die IPv4-Adresse des jeweils anderen PCs eintragen; auf beiden Seiten UDP-Port **48731**. Windows-Firewall ggf. für das private Netzwerk freigeben. Keine Router-Portweiterleitung erforderlich.
5. Unterschiedliche Prioritäten, z. B. **10** und **20**, einstellen. Niedrigere Zahl gewinnt bei nahezu gleichzeitigem Sprechbeginn.
6. Ducking-Restpegel und Schwellwerte einstellen, **Änderungen übernehmen**, für den automatischen Betrieb **Mit Windows starten** aktivieren. Beim nächsten Öffnen startet die Verbindung selbstständig; beim ersten Einrichten bei Bedarf **Starten** drücken.
7. Einzeln und gleichzeitig sprechen und die verbleibende Übertragung in der Voice-Anwendung prüfen.

Vorhandene Einstellungen bleiben unter `%LocalAppData%\RoomMute\config.json` erhalten. Ältere Konfigurationen ohne Ducking-Einstellung erhalten automatisch 20 %. Die persönliche config.json nicht zwischen PCs kopieren: jeder PC braucht seine eigene Client-ID. Alte Konfigurationen ohne Shortcut erhalten F8. Beide PCs sollten auf 1.6 aktualisiert werden; ältere Versionen muten weiterhin vollständig.

Für einen lokalen Pegeltest die Partner-IP leer lassen. Es wird dann keine Netzwerkverbindung oder automatische Absenkung aktiviert.

## Bedienung

- **Stop:** Audioüberwachung und UDP beenden, eigenen Ducking-Eingriff zurücknehmen.
- **Automation pausieren:** Pegel und Verbindung weiterlaufen lassen, Eingangspegel wiederherstellen.
- **Eingangspegel wiederherstellen:** eigenes Ducking lösen und Automation pausieren.
- **Mikrofon stummschalten:** ausdrücklich manuell muten. Aufheben dieses Mutes in Windows oder am Mikrofon.
- Fenster schließen/minimieren: optional in den Tray. Zum vollständigen Beenden das Tray-Menü verwenden.
- Windows-Autostart ist optional und benötigt keine Administratorrechte. Beim Start aus einem neuen Programmordner aktualisiert RoomMute den Autostartpfad, sofern die Option gespeichert aktiv ist.

## Erkennung und Netzwerk

WASAPI im Shared Mode; optionaler lokaler RNNoise-Filter und Sprachwahrscheinlichkeit vor RMS/dBFS-Auswertung. PCM 16/24/32 Bit und Float32 werden zu Mono umgerechnet und für die Erkennung auf 48 kHz interpoliert. Die Verarbeitung nutzt 480-Sample-Blöcke (10 ms); Zustände bleiben über Paketgrenzen erhalten. Standardwerte: Sprechschwelle −35 dBFS, Ruheschwelle −42 dBFS, Attack 40 ms, Release 350 ms.

Release stabilisiert die Ruhe-Erkennung und verzögert zusätzlich die Freigabe nach empfangenem Silent. Mit Standardwerten kann die Freigabe ungefähr 700 ms plus Netzwerklatenz nach Sprechende erfolgen.

First Speaker Wins vergleicht UTC-Sprechbeginn. Innerhalb von 50 ms entscheidet die Priorität, danach die stabile Client-ID. Windows-Uhren sollten synchron sein. Ohne First Speaker Wins erhält der Partner Vorrang; beide Clients können sich dann bei gleichzeitigem Sprechen gegenseitig absenken.

Vollständiger Status spätestens alle 500 ms; Hello und Heartbeat jede Sekunde; Timeout nach 3 Sekunden ohne ein neues gültiges Paket. Session-ID, Sequenznummer und Handshake verhindern die Übernahme veralteter Paketfolgen. Ein regelmäßiger Vollzustand korrigiert verlorene Sprechbeginn-/Sprechende-Pakete. Wiederherstellung bei Disconnect ist immer aktiv.

UDP wird nach Partner-IP und Port gefiltert, ist aber nicht authentifiziert oder verschlüsselt. Die Anwendung ist für ein vertrauenswürdiges LAN gedacht.

## Grenzen

- Pegelerkennung unterscheidet keine Sprecher. Mikrofonabstand, Richtcharakteristik und Threshold-Kalibrierung bleiben wichtig; eine vollständige Echo-Unterdrückung ist nicht garantiert.
- Ducking verändert den Windows-Eingangsregler für **alle Anwendungen**, die dieses Mikrofon verwenden. Je nach Treiber sinkt auch der von RoomMute gemessene Pegel. Sehr leise Sprache oder Unterbrechen während des Duckings kann daher unentdeckt bleiben.
- Die Windows-Pegelskala ist treiberabhängig; einige Geräte setzen sehr kleine Werte auf praktisch null. Es gibt keine künstliche Pegelkorrektur und keinen virtuellen Audiotreiber.
- Unbehandelte verwaltete Fehler und Windows-Sitzungsende versuchen eine Wiederherstellung. Bei erzwungenem Prozessabbruch, Stromausfall oder nicht mehr erreichbarem Gerät ist sie nicht garantiert. Dann den Eingangspegel in Windows prüfen.
- Externe Lautstärkeänderungen geben die Verantwortung ab. Änderungen ohne Windows-Benachrichtigung werden beim Restore zusätzlich durch Vergleich des angewendeten Pegels berücksichtigt.
- Große Paketverzögerungen und Netzpartitionen können vorübergehende Fehlentscheidungen verursachen.

## Build und Tests

Windows 10/11 und ein .NET SDK mit Unterstützung für net8.0-windows (SDK 8 oder neuer):

```powershell
dotnet restore RoomMute.sln
dotnet build RoomMute.sln -c Release
dotnet run --project RoomMute.Tests -c Release
dotnet run --project RoomMute -c Release
./publish.ps1
```

Die 67 Prüfungen laufen als ausführbares Testprojekt, nicht über `dotnet test`. Sie umfassen VAD, Priorität, Mute-Regressionsfälle, Ducking/Wiederherstellung, externe Änderungen, Treiber-Rundung, Sessionwechsel, Paketverlust/-reihenfolge und echten bidirektionalen UDP-Loopback, Push-to-talk-Wiederherstellung, automatische Startwiederholungen und einen später gestarteten beziehungsweise neu gestarteten Partner. Zusätzlich wurden der native Shortcut-Lebenszyklus und beide Dashboard-Sprachen bei beiden Fenstergrößen geprüft.

Das Publish-Skript erzeugt einen versionierten Ordner und ein ZIP unter `artifacts/`, einschließlich .NET-Laufzeit und Lizenzhinweisen. Für Restore/Publish kann Internet nötig sein; die fertige App funktioniert offline. Das Paket ist nicht codesigniert.

## Struktur und lokale Daten

- `RoomMute.Core/`: VAD, Arbitration, AutoMuteService (koordiniert nun Ducking), DuckOwnership und Peer-Zustandsautomat.
- `RoomMute/`: WPF/MVVM, Core Audio, UDP, Tray, Konfiguration und Logging.
- `RoomMute.Tests/`: Logik- und Netzwerkprüfungen.
- `docs/TESTPLAN.md`: praktische Abnahme auf zwei PCs.
- Einstellungen: `%LocalAppData%\RoomMute\config.json`.
- Logs: `%LocalAppData%\RoomMute\Logs\yyyy-MM-dd.log` (keine Audiodaten; keine automatische Aufbewahrungsgrenze).

## Abhängigkeiten

NAudio.Wasapi und NAudio.Core 2.3.0, MIT-Lizenz, netstandard2.0-kompatibler Zweig für .NET 8:

- [Paket und Frameworks](https://www.nuget.org/packages/NAudio.Wasapi/2.3.0)
- [NAudio 2.x](https://github.com/naudio/NAudio/tree/release/2.x)

Nicht enthalten: Autodiscovery, Gruppen, Pairing, Installer oder automatische Updates. Der echte Zwei-PC-Test mit euren Mikrofonen ist zusätzlich zu den automatisierten Prüfungen erforderlich.
