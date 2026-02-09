using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PIModTool.Lib;
using PIModTool.Lib.Types;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels
{
    public class PixEditorViewModel: MvxViewModel
    {
        public IMvxCommand OpenPixCommand => new MvxAsyncCommand(OpenPix);
        private IMessageService _messageService;
        private IMvxNavigationService _navigationService;
        private List<GenericFile>? _pixFiles;
        private GenericFile? _activeFile;
        private bool _busy;
        private string _loadingScreenMessage = "LOADING";
        private DRPMesh? _activeMesh;
        public List<GenericFile>? PixFiles
        {
            get { return _pixFiles; }
            set { 
                SetProperty(ref  _pixFiles, value);
                RaisePropertyChanged(() => LoadedFiles);
            }
        }

        public GenericFile? ActiveFile
        {
            get { return _activeFile; }
            set { 
                SetProperty(ref _activeFile, value);
                LoadModel();
            }
        }
        
        public bool Busy
        {
            get { return _busy; }
            set { SetProperty(ref _busy, value); }
        }

        public string LoadingScreenMessage
        {
            get { return _loadingScreenMessage; }
            set { SetProperty(ref _loadingScreenMessage, value); }
        }

        public DRPMesh? ActiveMesh
        {
            get { return _activeMesh; }
            set { SetProperty(ref _activeMesh, value); }
        }

        public PixEditorViewModel(IMessageService messageService, IMvxNavigationService navigationService)
        {
            _messageService = messageService;
            _navigationService = navigationService;
        }

        public bool LoadedFiles
        {
            get { return PixFiles != null; }
        }

        private async Task OpenPix()
        {
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select a .pix file", ".pix file|*.pix|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Busy = true;

            PixFiles = await Task.Run(async () =>
            {
                return await Lib.PixHandler.ReadPix(filePath);
            });

            if(PixFiles == null)
            {
                await _messageService.ShowErrorAsync("An error occurred extracting your file.");
                return;
            }

            PixFiles = PixFiles.Where(x => x.Type == FileType.DRP).ToList();

            Busy = false;
        }

        private async Task LoadModel()
        {
            if(ActiveFile == null)
            {
                return;
            }
            (List<MeshVertex>? vertices, int vertexSectionEnd) = PixHandler.GetVertexData(ActiveFile);
            if(vertices == null || vertexSectionEnd < 0)
            {
                await _messageService.ShowNotifAsync("PIModTool couldn't find any vertices in this file. If this is an FX file, this is to be expected.");
                vertices = new List<MeshVertex>();
                //return;
            }

            List<MeshFace>? faces = PixHandler.GetFaceData(ActiveFile, vertexSectionEnd, vertices.Count);

            if(faces == null)
            {
                await _messageService.ShowNotifAsync("PIModTool ran into an issue reading faces in this file. The model may display corrupted.");
                faces = new List<MeshFace>();
            }

            ActiveMesh = new DRPMesh(vertices.ToArray(), faces.ToArray());
        }

        public async Task ChangeView<TViewModel>() where TViewModel : MvxViewModel
        {
            await _navigationService.Navigate<TViewModel>();
        }

        public async Task<string?> ShowSaveOBJDialog()
        {
            if(ActiveFile == null) { return null; }
            return await _messageService.ShowSaveFileDialogAsync("Select a location to save the .OBJ file", Path.GetFileNameWithoutExtension(ActiveFile.Path) + ".obj", "OBJ file|*.obj|All files|*.*");
        }
    }
}
