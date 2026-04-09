using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Core.ViewModels;
using PIModTool.Lib.Types;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Media3D;

namespace PIModTool.Wpf.Views
{
    /// <summary>
    /// Interaction logic for PixEditorView.xaml
    /// </summary>
    public partial class PixEditorView : MvxWpfView<PixEditorViewModel>
    {
        public PixEditorView()
        {
            InitializeComponent();
        }

        private async void BackButton_OnClick(object sender, EventArgs args)
        {
            await ViewModel.ChangeView<MainViewModel>();
        }
    }
}
