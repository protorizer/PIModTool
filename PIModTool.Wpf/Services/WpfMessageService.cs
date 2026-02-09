using PIModTool.Lib.Types;
using PIModTool.Wpf.Utilities;
using PIModTool.Wpf.Views.Components;
using System.Windows;

public class WpfMessageService : IMessageService
{
    public Task ShowErrorAsync(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task ShowNotifAsync(string message)
    {
        MessageBox.Show(message, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task<(bool confirmed, string? name, FileType? type)> ShowNewFileDialogAsync()
    {
        // Show file dialog
        SmallFEditorNewFilePopupWindow dialog = new SmallFEditorNewFilePopupWindow()
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        bool? result = dialog.ShowDialog();

        if (result == true)
        {
            return Task.FromResult((true, dialog.FileName, dialog.FileType));
        }

        return Task.FromResult((false, (string?)(null), (FileType?)null));
    }

    public Task<string?> ShowOpenFileDialogAsync(string title)
    {
        return Task.FromResult(FileSystemUtilities.OpenFile(title, "All files|*.*"));
    }

    public Task<string?> ShowOpenFileDialogAsync(string title, string filter)
    {
        return Task.FromResult(FileSystemUtilities.OpenFile(title, filter));
    }

    public Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName)
    {
        return Task.FromResult(FileSystemUtilities.SaveFile(title, defaultFileName, "All files|*.*"));
    }

    public Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string filter)
    {
        return Task.FromResult(FileSystemUtilities.SaveFile(title, defaultFileName, filter));
    }

    public Task<string?> ShowSaveFolderDialogAsync(string title)
    {
        return Task.FromResult(FileSystemUtilities.OpenFolder(title));
    }
}