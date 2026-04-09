using HelixToolkit.Wpf;
using MvvmCross.Platforms.Wpf.Views;
using PIModTool.Lib.Types;
using PIModTool.Core.ViewModels.PixEditorSubviews;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PIModTool.Wpf.Utilities;

namespace PIModTool.Wpf.Views.PixEditorSubviews
{
    /// <summary>
    /// Interaction logic for DRPEditorView.xaml
    /// </summary>
    public partial class DRPEditorView : MvxWpfView<DRPEditorViewModel>
    {
        public DRPEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Clean up old ViewModel instance
            if (e.OldValue is DRPEditorViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModelPropertyChanged;
            }

            if (e.NewValue is DRPEditorViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModelPropertyChanged;
                OnMeshChanged(newViewModel.ActiveMesh);
            }
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ActiveMesh) && sender is DRPEditorViewModel viewModel)
            {
                OnMeshChanged(viewModel.ActiveMesh);
            }
        }

        private void OnMeshChanged(DRPMesh? meshData)
        {
            if (meshData == null)
            {
                //MeshPoints.Points = new Point3DCollection();
                MeshViewer.Content = new Model3DGroup();
                return;
            }

            //MeshPoints.Points = [.. meshData.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))];
            GeometryModel3D displayModel = MeshUtilities.BuildGeometry(meshData);

            MeshViewer.Content = displayModel;
        }
    }
}
