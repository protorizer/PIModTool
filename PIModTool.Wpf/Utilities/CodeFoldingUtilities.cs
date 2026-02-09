using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using PIModTool.Lib.Types;
using PIModTool.Wpf.Types;
using System.Windows;

namespace PIModTool.Wpf.Utilities
{
    public static class CodeFoldingUtilities
    {
        private static FoldingManager? _foldingManager;
        private static AbstractFoldingStrategy? _foldingStrategy;

        public static readonly DependencyProperty BindCodeFoldingProperty = DependencyProperty.RegisterAttached("BindCodeFolding", typeof(FileType), typeof(CodeFoldingUtilities), new PropertyMetadata(FileType.UnknownText, OnCodeFoldingChanged));

        public static void UpdateCodeFoldings(TextEditor editor)
        {
            if(_foldingStrategy != null && _foldingManager != null)
            {
                _foldingStrategy.UpdateFoldings(_foldingManager, editor.Document);
            }
        }

        public static void SetBindCodeFolding(DependencyObject d, FileType value)
        {
            d.SetValue(BindCodeFoldingProperty, value);
        }

        public static FileType GetBindCodeFolding(DependencyObject d)
        {
            return (FileType)d.GetValue(BindCodeFoldingProperty);
        }

        private static void OnCodeFoldingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor || e.NewValue is not FileType newType)
            {
                return;
            }

            UpdateCodeFoldingStrategy(editor, newType);
        }

        // Somewhat duplicate logic here - fix for crashing when switching between files of the same type because of FoldingManager not being uninstalled
        // Not very optitmized since it does the entire strategy logic too, make it better later
        public static void RefreshCodeFolding(TextEditor editor, FileType fileType)
        {
            UpdateCodeFoldingStrategy(editor, fileType);
        }

        private static void UpdateCodeFoldingStrategy(TextEditor editor, FileType fileType) {
            if (_foldingManager != null)
            {
                FoldingManager.Uninstall(_foldingManager);
            }
            _foldingManager = FoldingManager.Install(editor.TextArea);

            AbstractFoldingStrategy? foldingStrategy = fileType switch
            {
                FileType.XML => new MarkupFoldingStrategy(),
                FileType.ObjectScript => new ObjectScriptFoldingStrategy(),
                FileType.EventScript => new EventScriptFoldingStrategy(),
                _ => null
            };

            _foldingStrategy = foldingStrategy;

            if(foldingStrategy != null) {
                foldingStrategy.UpdateFoldings(_foldingManager, editor.Document);
            }
            else
            {
                _foldingManager.Clear();
                FoldingManager.Uninstall(_foldingManager);
                _foldingManager = null;
                _foldingStrategy = null;
            }
        }
    }
}
