using HelixToolkit.Wpf;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Lib.Types;
using PIModTool.Core.ViewModels.PixEditorSubviews;

namespace PIModTool.Wpf.Views.PixEditorSubviews
{
    /// <summary>
    /// Interaction logic for X2MEditorView.xaml
    /// </summary>
    public partial class TextureEditorView : MvxWpfView<TextureEditorViewModel>
    {
        public TextureEditorView()
        {
            InitializeComponent();
        }
    }
}
