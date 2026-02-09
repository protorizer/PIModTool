using PIModTool.Core.ViewModels.Components;
using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
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
using System.Windows.Shapes;

namespace PIModTool.Wpf.Views.Components
{
    /// <summary>
    /// Interaction logic for SmallFEditorNewFilePopupWindow.xaml
    /// </summary>
    /// Does not use MvXCross, keeps lightweight but maybe update later?
    public partial class SmallFEditorNewFilePopupWindow : Window
    {
        public SmallFEditorNewFilePopupViewModel ViewModel { get; }
        public string? FileName => ViewModel.FileName;
        public FileType? FileType => ViewModel.FileType;
        
        public SmallFEditorNewFilePopupWindow()
        {
            InitializeComponent();
            ViewModel = new SmallFEditorNewFilePopupViewModel();
            DataContext = ViewModel;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                MessageBox.Show("Please enter a valid file name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FileType == null)
            {
                MessageBox.Show("Please select a file type.", "Missing Type", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
