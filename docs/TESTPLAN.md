# Hardware-Abnahme 1.4

Beide PCs auf 1.4 aktualisieren; vorher alte Versionen über den Tray beenden.

1. Den ursprünglichen Windows-Eingangspegel notieren (z. B. 80 %).
2. Ducking-Restpegel 20 % speichern. Partner spricht: Eingangsregler sinkt ungefähr auf 16 %; Windows-Mute bleibt aus.
3. Partner hört auf: ursprüngliche 80 % nach Release wiederhergestellt.
4. Während Ducking Stop, Automation pausieren, Exit und Partner-Netzausfall testen: vorheriger Pegel wird wiederhergestellt, bei Netzausfall nach ca. 3 Sekunden.
5. Während Ducking manuell muten: Mute bleibt an, vorheriger Eingangspegel wird wiederhergestellt.
6. Während Ducking den Windows-Eingangspegel manuell ändern: neuer Wert bleibt erhalten und Automation pausiert.
7. Restpegel 1 %, 50 % und 100 % testen. 100 % verändert den Pegel nicht. Bei kleinen Werten auf Treiberrundung und Hörbarkeit achten.
8. Abwechselnd und gleichzeitig sprechen, Prioritäten 10/20 verwenden; in Discord/Teams tatsächliche Doppelübertragung prüfen.
9. Mikrofon abziehen und wieder anschließen: Fehler sichtbar; automatische Wiederholungen starten das gespeicherte Gerät nach dem Wiederanschließen erneut.
10. Fenster normal, maximiert und verkleinert ansehen: auch außerhalb der inneren Karten dunkler Hintergrund.
11. Tray, Autostart und gespeicherte Einstellungen prüfen.

Diese Hardware-Abnahme wurde nicht automatisch durchgeführt.


## Dashboard und Sprache

- Deutsch und Englisch oben rechts wechseln, Anwendung neu öffnen: Sprachwahl bleibt gespeichert.
- Während laufender Überwachung wechseln: Verbindung und Automation laufen weiter.
- Beide Schwellen verändern: grüne/blaue Markierungen folgen sofort; Änderungen erst nach Übernehmen aktiv.
- Attack-/Release-Fragezeichen mit der Maus berühren: verständliche Erklärung in der gewählten Sprache.
- Eigene IPv4 kopieren und auf dem Partner-PC eintragen. Bei mehreren Adaptern die LAN-Adresse auswählen.
- Dashboard in 1240 × 800 und 1040 × 740 prüfen: alle Regler, Felder und Übernehmen ohne Scrollen erreichbar.
- Noch nicht übernommene Felder beim Sprachwechsel beibehalten.

## Shortcut und automatischer Start

- Während der Partner spricht, F8 halten: eigener Eingangspegel wird wiederhergestellt; Loslassen aktiviert die normale Koordination erneut. In Discord/Teams bei aktivem Fenster sowie bei RoomMute im Tray prüfen.
- Manuelles Mute vor und während des Haltens: Taste darf es nicht aufheben. Extern veränderte Eingangspegel dürfen nicht überschrieben werden.
- Shortcut ändern und übernehmen; eine belegte Taste muss als nicht verfügbar erscheinen. Im Aufnahmedialog Shortcut deaktivieren wählen. Taste vor dem Beenden loslassen und danach erneut verwenden: keine hängen gebliebene Freigabe.
- Auf beiden PCs Mikrofon, Partner-IP und Mit Windows starten speichern; Automation aktiv lassen. Abmelden und anmelden: Überwachung und Verbindung starten ohne Klick auf Starten. Optional Minimiert starten verwenden.
- Partner erst später einschalten und später erneut starten: Verbindung stellt sich selbstständig her. Kein dauerhaftes Ducking während des Ausfalls.
- Beim Anmelden Mikrofon oder Netzwerk noch nicht verfügbar: Status zeigt den Wartezustand, danach wird automatisch erneut gestartet. Das gespeicherte Mikrofon muss erhalten bleiben.
- Während eines automatischen Startversuchs Stop drücken: keine weiteren Versuche bis zum nächsten bewussten Start oder Programmneustart.

Automatisiert geprüft: 67 Logik-/Netzwerktests, nativer Hotkey-Lebenszyklus ohne reale Tastatureingabe und Dashboard-Renderings DE/EN bei beiden Fenstergrößen. Eine echte Windows-Anmeldung und der Zwei-PC-Test mit physischen Mikrofonen bleiben Teil dieser Hardware-Abnahme.

## Freie Shortcut-Aufnahme

- Aufnehmen klicken, Strg halten und 5 drücken: Anzeige Strg + 5; erst nach Übernehmen global aktiv.
- Nur Strg/Alt/Umschalt drücken: Aufnahme wartet weiter. Esc oder Abbrechen erhält den bisherigen Shortcut. Wechsel zu einer anderen App während der Aufnahme bricht ab.
- Einzelne Taste, Alt + A, mehrere Modifikatoren und Nummernblock testen. Sprache wechseln: Strg/Ctrl-Anzeige aktualisiert sich.
- Während der Freigabe zuerst 5 oder zuerst Strg loslassen: in beiden Fällen endet die Freigabe. Manuelles Mute bleibt erhalten.
- Eine belegte Kombination wählen: nicht verfügbar anzeigen; andere aufnehmen. Deaktivieren und übernehmen entfernt die globale Belegung.

## Mouse3

- Aufnehmen anklicken und Mausrad drücken: Mouse3 erscheint. Linke/rechte Klicks müssen Abbrechen und Deaktivieren weiterhin bedienen.
- Übernehmen und bei aktivem Discord/Spiel sowie im Tray halten: eigene Absenkung endet beim Drücken und normale Koordination gilt nach Loslassen wieder.
- Strg + Mouse3 aufnehmen; zuerst Strg oder zuerst die mittlere Taste loslassen: Freigabe endet jeweils.
- Maus bewegen oder Mausrad drehen: kein Override. Manuelles Mute bleibt erhalten.
- Von Mouse3 auf Tastatur, anschließend deaktivieren: Registrierung wechselt ohne zurückbleibende Freigabe.

## Updates und Mikrofonpegel 1.5.0

- Mikrofonlautstärke mit Windows-Eingangsregler vergleichen, auch während Ducking. Ändern pausiert Automation, setzt den gewählten Wert sofort und lässt manuelles Mute erhalten.
- Während Ducking Windows herunterfahren/abmelden: beim nächsten Start ursprünglicher Pegel. Zusätzlich eine Instanz testweise hart beenden; nach Neustart muss der gespeicherte eigene Eingriff wiederhergestellt werden. Eine zwischenzeitliche externe Pegeländerung bleibt erhalten.
- Mikrofon nach fehlgeschlagenem Restore abziehen und wieder anschließen: Wiederherstellung bei nächstem Startversuch/Geräteauswahl; kein Wechsel auf ein anderes Aufnahmegerät.
- Update-Dialog zeigt installierte/neuste Version und Patchnotes. Ohne Internet oder bei API-Limit klarer Fehlerstatus; LAN/Audio laufen weiter.
- Neues stabiles Release für die passende Architektur veröffentlichen: Hinweis im Tool. Prereleases und ältere Versionen dürfen kein Update anbieten.
- Download mit Unterbrechung/falschem Hash testen: kein Start einer ungeprüften Version. Das vollständige geprüfte ZIP lässt sich über Downloadordner öffnen.
- Neue Version starten: eigene Pegelabsenkung wird zuerst zurückgenommen, alte Instanz endet, genau eine neue Instanz startet. Konfiguration und Sprache bleiben erhalten. Nach Anmeldung funktioniert Autostart aus dem neuen Ordner.
- Änderungen vor Updatewechsel übernehmen. Bei Restorefehler kein Wechsel; Wiederherstellungsdatei für späteren Versuch behalten.

Automatisiert: persistierte Neustart-Recovery, externe Änderungen, manuelles Mute, fehlgeschlagene Writes, Treiberrundung, Update-Metadaten, SHA-256, Größenprüfung, Abbruch und erneute Installationsprüfung. Ein echtes Herunterfahren sowie der Wechsel einer laufenden Hardware-Sitzung werden hier als manuelle Abnahme geprüft.

## Geräuschfilter 1.6

Automatisch geprüft: native RNNoise-DLL lädt und verarbeitet Stille/Rauschen, 16/24/32-Bit-PCM und Float32, Mehrkanal-Downmix, 8–192-kHz-Interpolation mit beliebig geteilten Paketen, Sprachwahrscheinlichkeit mit Attack/Release-Hysterese, Filter aus, alte Konfigurationen, Validierung und Lebenszyklus. Alle 75 Tests mit `dotnet run --project RoomMute.Tests -c Release`. Eine lokal per Windows-Sprachausgabe erzeugte PCM-Testdatei wird optional mit `--verify-detection-wave <wav>` geprüft; sie ist kein Ersatz für echte Mikrofonaufnahmen.

- KI-Geräuschfilter aktivieren, Übernehmen. Normal sprechen und dabei Pegel/Status beobachten. Dann nur an der Vape ziehen, atmen, Tastatur benutzen: der Partner soll möglichst nicht als Folge dieser Geräusche abgesenkt werden.
- Bei Fehlstarts Sprachsicherheit schrittweise von 60 auf 70/80 % erhöhen und übernehmen. Bei fehlenden leisen Worten verringern. Danach normale/leise Sprache, Zischlaute und Sprechpausen erneut prüfen.
- Filter ausschalten und übernehmen: bisherige Pegelerkennung und ungefilterte Mikrofonbar kehren zurück. Filterstatus/Sprachsicherheit bleiben über Neustart und Sprachwechsel gespeichert; Entwürfe brauchen Übernehmen.
- Discord/Teamspeak vor/nach dem Einschalten vergleichen: Filter verändert deren Audio nicht und erzeugt kein virtuelles Mikrofon. Ducking wirkt weiterhin auf den Windows-Eingang.
- Push-to-talk während Filter-Stille halten: bewusste Freigabe funktioniert. Manuelles Mute bleibt erhalten. Stop, Gerätewechsel, fehlende DLL und Abziehen des Mikrofons müssen kontrolliert wiederherstellen/stoppen.
- Task-Manager während normaler Sprache/Vape und bei Stop prüfen. Der Testrechner verarbeitete 3 s künstliches Rauschen in ca. 0,28 s; das ist kein allgemeiner CPU-Prozentwert.

Echte Vape-Geräusche, reale Mikrofon-Sprachqualität und Dauerlast auf beiden Nutzer-PCs wurden nicht automatisch geprüft.
