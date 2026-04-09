using MvvmCross.ViewModels;
using PIModTool.Core.Services;
using PIModTool.Lib;
using PIModTool.Lib.Types;

namespace PIModTool.Core.ViewModels.PixEditorSubviews
{
    public class DRPEditorViewModel : MvxViewModel, IPixEditorSubviewViewModel
    {
        private readonly IMessageService _messageService;
        private readonly IMeshExportService _meshExportService;

        private GenericFile? _activeFile;
        private DRPMesh? _activeMesh;

        public DRPEditorViewModel(IMessageService messageService, IMeshExportService meshExportService)
        {
            _messageService = messageService;
            _meshExportService = meshExportService;
        }

        public GenericFile? ActiveFile
        {
            get { return _activeFile; }
            set
            {
                SetProperty(ref _activeFile, value);
                LoadModel();
            }
        }

        public DRPMesh? ActiveMesh
        {
            get { return _activeMesh; }
            set { SetProperty(ref _activeMesh, value); }
        }

        public void SetActiveFile(GenericFile? file)
        {
            ActiveFile = file;
        }

        public async Task ExportDataAsync()
        {
            if (ActiveMesh == null)
            {
                await _messageService.ShowNotifAsync("No model selected to export.");
                return;
            }

            string? filePath = await _messageService.ShowSaveFileDialogAsync("Save as OBJ file", Path.GetFileNameWithoutExtension(ActiveFile.Path) + ".obj", "OBJ file|*.obj|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            MeshExportOptions meshExportOptions = new MeshExportOptions
            {
                ExportNormals = false,
                SwitchYZ = true,
                MaterialsFile = Path.ChangeExtension(filePath, ".mtl")
            };

            bool res = await _meshExportService.ExportObjAsync(ActiveMesh, filePath, meshExportOptions);

            if (res)
            {
                await _messageService.ShowNotifAsync($"Model exported successfully to {Path.GetFileName(filePath)}");
            }
            else
            {
                await _messageService.ShowErrorAsync("Failed to export model. Please check the file path and try again.");
            }
        }

        private async Task LoadModel()
        {
            if (ActiveFile == null)
            {
                return;
            }
            (List<MeshVertex>? vertices, int vertexSectionEnd) = DRPHandler.GetVertexData(ActiveFile);
            if (vertices == null || vertexSectionEnd < 0)
            {
                await _messageService.ShowNotifAsync("PIModTool couldn't find any vertices in this file. If this is an FX file, this is to be expected.");
                vertices = new List<MeshVertex>();
            }

            List<MeshFace>? faces = DRPHandler.GetFaceData(ActiveFile, vertexSectionEnd, vertices.Count);

            if (faces == null)
            {
                await _messageService.ShowNotifAsync("PIModTool ran into an issue reading faces in this file. The model may display corrupted.");
                faces = new List<MeshFace>();
            }

            ActiveMesh = new DRPMesh(vertices.ToArray(), faces.ToArray());
        }
    }
}
