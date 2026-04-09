
using PIModTool.Lib.Types;

namespace PIModTool.Core.Services
{
    public interface IMeshExportService
    {
        Task<bool> ExportObjAsync(DRPMesh mesh, string filePath, MeshExportOptions? exportOptions = null);
    }

    public class MeshExportOptions
    {
        public bool ExportNormals { get; set; } = false;
        public bool SwitchYZ { get; set; } = true;
        public string? MaterialsFile { get; set; }
    }
}
