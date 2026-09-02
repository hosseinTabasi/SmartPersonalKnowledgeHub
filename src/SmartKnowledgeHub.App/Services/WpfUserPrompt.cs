using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace SmartKnowledgeHub.App.Services;

public sealed class WpfUserPrompt : IUserPrompt
{
    public string? OpenFile(string filter)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
            Title = "Register a local file"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool Confirm(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public void Alert(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void OpenInOs(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Alert("No path is stored for this file.", "Files");
            return;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Alert("The path no longer exists on this computer:\n" + path, "Files");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
