using System.Drawing;
using System.Windows.Input;
using System.Windows.Threading;
using RoomMute.ViewModels;
using Forms = System.Windows.Forms;
namespace RoomMute.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon icon;
    private readonly Forms.ToolStripMenuItem status = new() { Enabled = false };
    private readonly Forms.ToolStripMenuItem automationStatus = new() { Enabled = false };
    private readonly DispatcherTimer timer;
    private readonly List<(Forms.ToolStripItem Item, string Key)> labels = new();
    private readonly MainViewModel model;
    public TrayService(MainViewModel model, Action open, Action exit)
    {
        this.model = model;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(new Forms.ToolStripMenuItem("RoomMute") { Enabled = false });
        menu.Items.Add(status); menu.Items.Add(automationStatus);
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add(menu, "Open", open);
        Add(menu, "Enable", () => { if (!model.AutomationEnabled) model.ToggleCommand.Execute(null); });
        Add(menu, "Pause", () => { if (model.AutomationEnabled) model.ToggleCommand.Execute(null); });
        AddCommand(menu, "Mute", model.MuteCommand);
        AddCommand(menu, "Restore", model.RestoreCommand);
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add(menu, "Exit", exit);
        icon = new Forms.NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application,
            Text = "RoomMute", ContextMenuStrip = menu, Visible = true
        };
        icon.DoubleClick += (_, _) => open();
        timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => Update();
        UiText.Current.PropertyChanged += LanguageChanged;
        Update(); timer.Start();
    }
    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Update();
    private void Update()
    {
        foreach (var (item, key) in labels) item.Text = UiText.Current[key];
        status.Text = model.ConnectionText;
        automationStatus.Text = model.AutomationText;
        string text = "RoomMute · " + model.MicrophoneState;
        icon.Text = text[..Math.Min(63, text.Length)];
    }
    private void Add(Forms.ContextMenuStrip menu, string key, Action action)
    {
        var item = menu.Items.Add(UiText.Current[key], null, (_, _) => action());
        labels.Add((item, key));
    }
    private void AddCommand(Forms.ContextMenuStrip menu, string key, ICommand command) =>
        Add(menu, key, () => { if (command.CanExecute(null)) command.Execute(null); });
    public void Dispose()
    {
        UiText.Current.PropertyChanged -= LanguageChanged;
        timer.Stop();
        icon.Visible = false;
        icon.ContextMenuStrip?.Dispose();
        icon.Icon?.Dispose();
        icon.Dispose();
    }
}

