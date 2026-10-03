namespace RoomMute.Services;

public sealed class LogService
{
    private readonly object gate = new();
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoomMute");
    public void Write(string message, Exception? exception = null)
    {
        try
        {
            lock (gate)
            {
                string directory = Path.Combine(DataDirectory, "Logs");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, $"{DateTime.Today:yyyy-MM-dd}.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} {message}{(exception is null ? "" : " | " + exception)}{Environment.NewLine}");
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
