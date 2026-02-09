using MvvmCross.Platforms.Wpf.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PIModTool.Wpf.Utilities;
using PIModTool.Core.ViewModels;

namespace PIModTool.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : MvxWpfView
    {
        public MainView()
        {
            InitializeComponent();
        }

        private async void SmallFEditor_OnClick(object sender, EventArgs args)
        {
            // Open file dialog to pick SmallF.dat, send it to ViewModel that sends it off to core ETC, wait for validation and switch views
            string? filePath = FileSystemUtilities.OpenFile("Open SmallF.dat", "SmallF.dat|*.dat|All files|*.*");

            if (this.DataContext is MainViewModel viewModel)
            {
                await viewModel.OpenSmallF(filePath);
            }
        }

        private void SmallFPacker_OnClick(object sender, EventArgs args)
        {
            // Open folder and file select dialog, send the paths to ViewModel, wait for validation, send success dialog
            string? folderPath = FileSystemUtilities.OpenFolder("Select folder to pack into SmallF.dat");
            if (!string.IsNullOrEmpty(folderPath))
            {
                string? filePath = FileSystemUtilities.SaveFile("Select location to save SmallF.dat", "SmallF.dat", ".dat|*.dat|All files|*.*");
                int? result = (this.DataContext as MainViewModel)?.PackSmallF(folderPath, filePath);
                if (result != 0)
                {
                    if (result == 2)
                    {
                        MessageBox.Show("Could not access files in the folder, or the folder is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (result == 3)
                    {
                        MessageBox.Show("Error saving file. Make sure you have permission to write to the location, and that the file is not currently in use.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Successfully packed files!", "", MessageBoxButton.OK);
                }
            }

        }

        private async void YukEditor_OnClick(object sender, EventArgs args)
        {
            // Open the yuk editor view. Opening an existing yuk file will be handled by the editor itself, since by default you create new yuk files.

            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.ChangeView<YukEditorViewModel>();
            }
        }

        private async void PixEditor_OnClick(object sender, EventArgs args)
        {
            if(DataContext is MainViewModel viewModel)
            {
                await viewModel.ChangeView<PixEditorViewModel>();
            }
        }
    }
}
