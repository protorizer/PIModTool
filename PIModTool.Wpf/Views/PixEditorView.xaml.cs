using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Core.ViewModels;
using PIModTool.Lib.Types;
using PIModTool.Wpf.Utilities;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private async void SavePixButton_OnClick(object sender, EventArgs args)
        {
            if (!ViewModel.LoadedFiles)
            {
                return;
            }
            string? filePath = FileSystemUtilities.SaveFile("Select location to save .pix file", ViewModel.FileName, ".pix file|*.pix|All files|*.*");
            if (!string.IsNullOrEmpty(filePath))
            {
                await ViewModel.SavePix(filePath);
            }
        }
    }
}
