using Microsoft.Win32;
namespace RoomMute.Services;

public static class StartupService
{
    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled)
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Programmpfad fehlt.");
            if (!Path.GetFileName(executable).Equals("RoomMute.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Autostart bitte aus RoomMute.exe konfigurieren.");
            key.SetValue("RoomMute", "\"" + executable + "\"");
        }
        else key.DeleteValue("RoomMute", false);
    }
}
