using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RoomMute.ViewModels;
namespace RoomMute.Views;

public sealed class UpdateWindow : Window
{
    public UpdateWindow(MainViewModel model)
    {
        DataContext = model; Title = model.Text["Updates"]; Width = 720; Height = 600; MinWidth = 640; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
        Background = (Brush)Application.Current.FindResource("BackgroundBrush");
        Foreground = (Brush)Application.Current.FindResource("TextBrush"); FontFamily = new FontFamily("Segoe UI");
        var grid = new Grid { Margin = new Thickness(22) };
        foreach (var height in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto })
            grid.RowDefinitions.Add(new RowDefinition { Height = height });
        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var versions = new TextBlock { FontSize = 16, FontWeight = FontWeights.Bold };
        versions.SetBinding(TextBlock.TextProperty, new Binding(nameof(model.UpdateVersions))); heading.Children.Add(versions);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        status.SetBinding(TextBlock.TextProperty, new Binding(nameof(model.UpdateStatus))); heading.Children.Add(status);
        grid.Children.Add(heading);
        var privacy = new TextBlock { Text = model.Text["UpdatePrivacy"], TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(privacy, 1); grid.Children.Add(privacy);
        var notes = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0), Padding = new Thickness(12), FontSize = 13 };
        notes.SetBinding(TextBox.TextProperty, new Binding(nameof(model.PatchNotes)) { Mode = BindingMode.OneWay }); Grid.SetRow(notes, 2); grid.Children.Add(notes);
        var progress = new ProgressBar { Maximum = 100, Height = 5, Margin = new Thickness(0, 12, 0, 12) };
        progress.SetBinding(ProgressBar.ValueProperty, new Binding(nameof(model.UpdateProgress)) { Mode = BindingMode.OneWay }); Grid.SetRow(progress, 3); grid.Children.Add(progress);
        var buttons = new WrapPanel();
        foreach (var entry in new[] { ("UpdateCheck", model.CheckUpdateCommand), ("UpdateDownload", model.DownloadUpdateCommand), ("UpdateInstall", model.InstallUpdateCommand), ("UpdateFolder", model.OpenUpdateFolderCommand) })
            buttons.Children.Add(new Button { Content = model.Text[entry.Item1], Command = entry.Item2, FontSize = 11, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 8, 6) });
        Grid.SetRow(buttons, 4); grid.Children.Add(buttons);
        Content = grid;
    }
}


