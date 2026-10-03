using System.Text.Json;
namespace RoomMute.Services;

public sealed class ConfigurationService(LogService log)
{
    public string? LoadWarning { get; private set; }
    private string ConfigPath => Path.Combine(LogService.DataDirectory, "config.json");
    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath)) return new();
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), NetworkMessage.JsonOptions)
                ?? throw new InvalidDataException("Leere Konfiguration.");
            if (config.Validate(false) is { } error) throw new InvalidDataException(error);
            return config;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            log.Write("Configuration could not be loaded", ex);
            LoadWarning = "Konfiguration konnte nicht geladen werden. Standardwerte sind aktiv; bitte Einstellungen prüfen.";
            return new();
        }
    }
    public void Save(AppConfig config)
    {
        if (config.Validate(false) is { } error) throw new InvalidDataException(error);
        Directory.CreateDirectory(LogService.DataDirectory);
        string temporary = ConfigPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(config, new JsonSerializerOptions(NetworkMessage.JsonOptions) { WriteIndented = true }));
        File.Move(temporary, ConfigPath, true);
    }
}
