
using HelixToolkit.Wpf;
using PIModTool.Core.Services;
using PIModTool.Lib.Types;
using PIModTool.Wpf.Utilities;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Media3D;

namespace PIModTool.Wpf.Services
{
    public class WpfMeshExportService: IMeshExportService
    {
        public async Task<bool> ExportObjAsync(DRPMesh mesh, string filePath, MeshExportOptions? options = null)
        {
            if (mesh == null)
            {
                return false;
            }

            if (options == null)
            {
                options = new MeshExportOptions();
            }

            try
            {
                GeometryModel3D geometry = MeshUtilities.BuildGeometry(mesh);
                ObjExporter exporter = new ObjExporter
                {
                    MaterialsFile = Path.ChangeExtension(filePath, ".mtl"),
                    ExportNormals = false,
                    SwitchYZ = true,
                };

                using (FileStream file = File.Create(filePath))
                {
                    exporter.Export(geometry, file);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
