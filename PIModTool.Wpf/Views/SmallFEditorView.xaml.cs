using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Search;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Core.ViewModels;
using PIModTool.Lib.Types;
using PIModTool.Wpf.Services;
using PIModTool.Wpf.Utilities;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PIModTool.Wpf.Views
{
    /// <summary>
    /// Interaction logic for SmallFEditorView.xaml
    /// </summary>
    public partial class SmallFEditorView : MvxWpfView
    {
        private SyntaxMarkerService _syntaxMarkerService;
        private DispatcherTimer _codeProcessingTimer;

        private static SearchPanel? _searchPanel;

        public SmallFEditorView()
        {
            InitializeComponent();
            PSCEditor.Loaded += (object sender, RoutedEventArgs e) =>
                {
                    // Disable default indentation handling - it's handled in PreviewKeyDown
                    PSCEditor.TextArea.IndentationStrategy = null;
                    _syntaxMarkerService = new SyntaxMarkerService(PSCEditor.TextArea.TextView);
                    (DataContext as SmallFEditorViewModel).SyntaxErrors.CollectionChanged += (s, e) => RefreshMarkers();

                    //_searchPanel = SearchPanel.Install(PSCEditor.TextArea);

                    SetupTimer();

                    // Uninstall searchPanel before Document changes to avoid a crash
                    PSCEditor.TextArea.DocumentChanged += (_, __) =>
                    {
                        _searchPanel?.Uninstall();
                    };

                    PSCEditor.DocumentChanged += (_, __) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _searchPanel = SearchPanel.Install(PSCEditor.TextArea);
                        }), DispatcherPriority.Background);
                        SetupTimer();
                        // Bit of a hacky fix - update later
                        CodeFoldingUtilities.RefreshCodeFolding(PSCEditor, (DataContext as SmallFEditorViewModel)?.ActiveFileType ?? FileType.UnknownText);
                        (DataContext as SmallFEditorViewModel)?.ValidateSyntax();
                    };

                    PSCEditor.TextArea.TextEntered += (_, e) =>
                    {
                        if (e.Text.Trim().Length == 0)
                        {
                            return;
                        }
                        DocumentLine line = PSCEditor.Document.GetLineByOffset(PSCEditor.CaretOffset);
                        string currentLine = PSCEditor.Document.GetText(line.Offset, line.Length);
                        CodeFormattingAction action = (DataContext as SmallFEditorViewModel).GetCodeFormattingAction(currentLine, e.Text);
                        int caretPosition = PSCEditor.CaretOffset;
                        if (action.PreCursorText != null)
                        {
                            caretPosition += action.PreCursorText.Length;
                            PSCEditor.TextArea.PerformTextInput(action.PreCursorText);
                        }
                        if (action.PostCursorText != null)
                        {
                            PSCEditor.TextArea.PerformTextInput(action.PostCursorText);
                        }
                        PSCEditor.CaretOffset = caretPosition;
                        e.Handled = true;
                    };

                    PSCEditor.TextArea.PreviewKeyDown += (_, e) =>
                    {
                        if (e.Key != Key.Enter) { return; }
                        DocumentLine line = PSCEditor.Document.GetLineByOffset(PSCEditor.CaretOffset);
                        string currentLine = PSCEditor.Document.GetText(line.Offset, line.Length);

                        CodeFormattingAction action = (DataContext as SmallFEditorViewModel).GetCodeFormattingAction(currentLine, "\n");

                        int caretPosition = PSCEditor.CaretOffset;
                        if (action.PreCursorText != null)
                        {
                            PSCEditor.TextArea.PerformTextInput(action.PreCursorText);
                            caretPosition = PSCEditor.CaretOffset;
                        }
                        if (action.PostCursorText != null)
                        {
                            PSCEditor.TextArea.PerformTextInput(action.PostCursorText);
                        }
                        PSCEditor.CaretOffset = caretPosition;
                        e.Handled = true;
                    };

                };
        }

        private void SetupTimer()
        {
            if (PSCEditor.Document == null) { return; }

            _codeProcessingTimer?.Stop();

            _codeProcessingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };

            // To avoid nuking performance on huge files
            if (PSCEditor.Document.TextLength > 50000)
                _codeProcessingTimer.Interval = TimeSpan.FromSeconds(2);

            _codeProcessingTimer.Tick += (_, __) =>
            {
                _codeProcessingTimer.Stop();
                // Save file contents - do this first before potentially long-running validations
                (DataContext as SmallFEditorViewModel)?.SaveDocumentToFile((DataContext as SmallFEditorViewModel)?.ActiveFile);
                (DataContext as SmallFEditorViewModel)?.ValidateSyntax();
                // Update code folds
                CodeFoldingUtilities.UpdateCodeFoldings(PSCEditor);
            };

            // When text changes, restart debounce timer
            PSCEditor.Document.TextChanged -= ResetTimer;
            PSCEditor.Document.TextChanged += ResetTimer;
        }

        private void ResetTimer(object? sender, EventArgs e)
        {
            _codeProcessingTimer.Stop();
            _codeProcessingTimer.Start();
        }

        private void RefreshMarkers()
        {
            if (_syntaxMarkerService == null) { return; }

            _syntaxMarkerService.RemoveAll();

            foreach (var err in (DataContext as SmallFEditorViewModel).SyntaxErrors)
            {
                try
                {
                    var line = PSCEditor.Document.GetLineByNumber(err.Line);
                    int startOffset = line.Offset + Math.Max(0, err.Column - 1);
                    Debug.Print("WRITING MARKER OVER CHAR: " + PSCEditor.Document.Text[startOffset]);
                    _syntaxMarkerService.Create(
                        startOffset,
                        err.Length,
                        Colors.Red,
                        err.Message
                    );
                }
                catch { /* Ignore invalid offsets */ }
            }
        }

        private void FileNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is SmallFFile file)
            {
                var vm = (SmallFEditorViewModel)DataContext;
                if (e.Key == Key.Enter)
                    vm.EndRenameCommand.Execute(file);
                else if (e.Key == Key.Escape)
                    vm.RenamingFile = null;
            }
        }

        private void FileNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is SmallFFile file)
            {
                var vm = (SmallFEditorViewModel)DataContext;
                vm.EndRenameCommand.Execute(file);
            }
        }

        private void FileNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void ExportFilesButton_OnClick(object sender, EventArgs args)
        {
            string? folderPath = FileSystemUtilities.OpenFolder("Select folder to extract contents");
            if (!string.IsNullOrEmpty(folderPath))
            {
                (DataContext as SmallFEditorViewModel).ExportAllFiles(folderPath);
            }
        }

        private void SaveSmallFButton_OnClick(object sender, EventArgs args)
        {
            string? filePath = FileSystemUtilities.SaveFile("Select location to save SmallF", (DataContext as SmallFEditorViewModel).FileName, "SmallF.dat|*.dat|All files|*.*");

            if (!string.IsNullOrEmpty(filePath))
            {
                (DataContext as SmallFEditorViewModel).SaveSmallF(filePath);
            }
        }

        private async void BackButton_OnClick(object sender, EventArgs args)
        {
            await (DataContext as SmallFEditorViewModel).ChangeView<MainViewModel>();
        }
    }
}
