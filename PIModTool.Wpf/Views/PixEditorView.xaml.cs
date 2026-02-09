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
            Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.ActiveMesh))
            {
                OnMeshChanged(ViewModel.ActiveMesh);
            }
        }

        private void OnMeshChanged(DRPMesh? meshData)
        {
            if (meshData == null)
            {
                //MeshPoints.Points = new Point3DCollection();
                MeshViewer.Content = new Model3DGroup();
            }
            else
            {
                //MeshPoints.Points = [.. meshData.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))];
                System.Windows.Media.Media3D.MeshGeometry3D model = new System.Windows.Media.Media3D.MeshGeometry3D();
                for (int i = 0; i < meshData.Vertices.Count; i++)
                {
                    model.Positions.Add(new Point3D(meshData.Vertices[i].X, meshData.Vertices[i].Y, meshData.Vertices[i].Z));
                }
                for(int i = 0; i < meshData.Faces.Count; i++)
                {
                    if(i % 2 == 0)
                    {
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[0]);
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[1]);
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[2]);
                    }
                    else
                    {
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[0]);
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[2]);
                        model.TriangleIndices.Add(meshData.Faces[i].Indices[1]);
                    }
                }

                GeometryModel3D displayModel = new GeometryModel3D { Geometry = model, Material = Materials.Gray, BackMaterial = Materials.Gray };

                MeshViewer.Content = displayModel;
            }
        }

        private async void BackButton_OnClick(object sender, EventArgs args)
        {
            await ViewModel.ChangeView<MainViewModel>();
        }

        private async void ExportObjButton_OnClick(object sender, EventArgs args)
        {
            string? filePath = await ViewModel.ShowSaveOBJDialog();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            ObjExporter exporter = new ObjExporter
            {
                MaterialsFile = Path.ChangeExtension(filePath, ".mtl"),
                ExportNormals = false,
                SwitchYZ = true,
            };

            using (FileStream file = File.Create(filePath))
            {
                exporter.Export(MeshViewer.Content, file);
            }
        }
    }
}
