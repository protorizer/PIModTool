using PIModTool.Lib.Types;

public interface IMessageService
{
    Task ShowErrorAsync(string message);
    Task ShowNotifAsync(string message);
    Task<(bool confirmed, string? name, FileType? type)> ShowNewFileDialogAsync();
    Task<string?> ShowOpenFileDialogAsync(string title);
    Task<string?> ShowOpenFileDialogAsync(string title, string filter);
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName);
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string filter);
    Task<string?> ShowSaveFolderDialogAsync(string title);
}