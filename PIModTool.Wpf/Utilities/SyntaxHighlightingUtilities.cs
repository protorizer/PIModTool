using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PIModTool.Lib.Types;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace PIModTool.Wpf.Utilities
{
    public static class SyntaxHighlightingUtilities
    {
        private static void RegisterDependencies()
        {
            if (HighlightingManager.Instance.GetDefinition("ObjectScript") != null)
            {
                return;
            }

            using (Stream s = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/SyntaxDefinitions/ObjectScriptHighlighting.xshd")).Stream) {
                using (XmlReader reader = XmlReader.Create(s)) {
                    var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("ObjectScript", new[] { ".obj" }, definition);
                }
            }

            using (Stream s = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/SyntaxDefinitions/EventScriptHighlighting.xshd")).Stream)
            {
                using (XmlReader reader = XmlReader.Create(s))
                {
                    var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("EventScript", new[] { ".evt" }, definition);
                }
            }

            using (Stream s = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/SyntaxDefinitions/PSCScriptHighlighting.xshd")).Stream)
            {
                using (XmlReader reader = XmlReader.Create(s))
                {
                    var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("PSCScript", new[] { ".psc" }, definition);
                }
            }
        }

        public static readonly DependencyProperty BindHighlightingProperty =
        DependencyProperty.RegisterAttached(
            "BindHighlighting",
            typeof(object),
            typeof(SyntaxHighlightingUtilities),
            new PropertyMetadata(null, OnHighlightingChanged));

        public static void SetBindHighlighting(DependencyObject obj, object value)
        => obj.SetValue(BindHighlightingProperty, value);

        public static object GetBindHighlighting(DependencyObject obj)
            => obj.GetValue(BindHighlightingProperty);

        private static void OnHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor || e.NewValue == null)
            {
                return;
            }

            RegisterDependencies();

            var fileType = e.NewValue;

            if (fileType is not FileType)
            {
                return;
            }

            IHighlightingDefinition? highlighting = fileType switch
            {
                FileType.XML =>  HighlightingManager.Instance.GetDefinition("XML") ,
                FileType.ObjectScript => HighlightingManager.Instance.GetDefinition("ObjectScript"),
                FileType.EventScript => HighlightingManager.Instance.GetDefinition("EventScript"),
                FileType.PSCScript => HighlightingManager.Instance.GetDefinition("PSCScript"),
                FileType.UnknownText => null,
                _ => null,
            };

            // Standard colors are difficult to see on the dark bg
            if (highlighting == HighlightingManager.Instance.GetDefinition("XML"))
            {
                foreach (var rule in highlighting.NamedHighlightingColors)
                {
                    switch (rule.Name)
                    {
                        case "XmlTag":
                            rule.Foreground = new SimpleHighlightingBrush(Colors.LightSkyBlue);
                            break;
                        case "AttributeValue":
                            rule.Foreground = new SimpleHighlightingBrush(Colors.Orange);
                            break;
                        case "AttributeName":
                            rule.Foreground = new SimpleHighlightingBrush(Colors.LightGray);
                            break;
                        case "Text":
                            rule.Foreground = new SimpleHighlightingBrush(Colors.White);
                            break;
                        case "XmlDeclaration":
                            rule.Foreground = new SimpleHighlightingBrush(Colors.Gray);
                            break;
                    }
                }
            }
            // Change link highlighting color
            editor.TextArea.TextView.LinkTextForegroundBrush = Brushes.LightBlue;
            editor.SyntaxHighlighting = highlighting;
        }
    }
}
