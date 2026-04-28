using MvvmCross.Commands;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PIModTool.Core.Types;
using PIModTool.Core.ViewModels.PixEditorSubviews;
using PIModTool.Lib;
using PIModTool.Lib.Types;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels
{
    public class PixEditorViewModel: MvxViewModel
    {
        // Services
        private readonly IMessageService _messageService;
        private readonly IMvxNavigationService _navigationService;
        private readonly IMvxIoCProvider _iocProvider;

        // Fields
        private List<GenericFile>? _pixFiles = null;
        private List<GenericFile>? _displayedFiles;
        private GenericFile? _activeFile;
        private PixEditorFileType? _selectedEditor;
        private MvxViewModel? _activeSubViewModel;
        private bool _busy;
        private string _loadingScreenMessage = "LOADING";

        // Commands
        public IMvxCommand OpenPixCommand => new MvxAsyncCommand(OpenPix);

        private IMvxAsyncCommand? _exportDataCommand;
        public IMvxCommand ExportDataCommand => _exportDataCommand ??= new MvxAsyncCommand(ExportData, () => ActiveSubViewModel is IPixEditorSubviewViewModel && ActiveFile != null);

        public List<GenericFile>? DisplayedFiles
        {
            get { return _displayedFiles; }
            set { 
                SetProperty(ref  _displayedFiles, value);
                RaisePropertyChanged(() => LoadedFiles);
            }
        }

        public bool LoadedFiles
        {
            get { return _pixFiles != null; }
        }

        public GenericFile? ActiveFile
        {
            get { return _activeFile; }
            set { 
                SetProperty(ref _activeFile, value);
                
                if(_activeSubViewModel is IPixEditorSubviewViewModel vm)
                {
                    vm.SetActiveFile(value);
                }

                ExportDataCommand.RaiseCanExecuteChanged();
            }
        }

        public List<PixEditorFileType> Editors { get; }
        public PixEditorFileType? SelectedEditor
        {
            get { return _selectedEditor; }
            set
            {
                SetProperty(ref _selectedEditor, value);
                ActiveFile = null;

                FilterFiles();
                UpdateSubViewModel();
            }
        }

        public MvxViewModel? ActiveSubViewModel
        {
            get { return _activeSubViewModel; }
            set
            {
                SetProperty(ref _activeSubViewModel, value);
                RaisePropertyChanged(nameof(HasActiveSubView));
                ExportDataCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasActiveSubView
        {
            get { return ActiveSubViewModel != null; }
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

        public async Task ChangeView<TViewModel>() where TViewModel : MvxViewModel
        {
            await _navigationService.Navigate<TViewModel>();
        }

        public PixEditorViewModel(IMessageService messageService, IMvxNavigationService navigationService, IMvxIoCProvider ioCProvider)
        {
            _messageService = messageService;
            _navigationService = navigationService;
            _iocProvider = ioCProvider;

            Editors = new List<PixEditorFileType> {
                new PixEditorFileType(".drp (3D Model) (WIP)", FileType.DRP, _iocProvider.IoCConstruct<DRPEditorViewModel>()),
                new PixEditorFileType(".p3m (PS3 Texture)", FileType.P3M, _iocProvider.IoCConstruct<TextureEditorViewModel>()),
                new PixEditorFileType(".x2m (Xbox 360 Texture)", FileType.X2M, _iocProvider.IoCConstruct<TextureEditorViewModel>())
            };
        }

        private async Task OpenPix()
        {
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select a .pix file", ".pix file|*.pix|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Busy = true;

            List<GenericFile>? files = await Task.Run(async () =>
            {
                return await PixHandler.ReadPix(filePath);
            });

            if(files == null)
            {
                await _messageService.ShowErrorAsync("An error occurred extracting your file.");
                return;
            }

            _pixFiles = files;

            SelectedEditor = null;
            FilterFiles();
            await RaisePropertyChanged(() => LoadedFiles);

            Busy = false;
        }

        private void FilterFiles()
        {
            if(_pixFiles == null)
            {
                DisplayedFiles = null;
                return;
            }

            if(SelectedEditor == null)
            {
                DisplayedFiles = null;
                return;
            }

            DisplayedFiles = _pixFiles.Where(x => x.Type == SelectedEditor.FileType).ToList();
        }

        private void UpdateSubViewModel()
        {
            if(SelectedEditor == null)
            {
                ActiveSubViewModel = null;
                return;
            }

            IPixEditorSubviewViewModel editorViewModel = SelectedEditor.SubViewModel;
            editorViewModel.SetActiveFile(ActiveFile);
            ActiveSubViewModel = editorViewModel as MvxViewModel;
        }

        private async Task ExportData()
        {
            if(ActiveSubViewModel is IPixEditorSubviewViewModel editorViewModel)
            {
                await editorViewModel.ExportDataAsync();
            }
        }

        public async Task<string?> ShowSaveOBJDialog()
        {
            if(ActiveFile == null) { return null; }
            return await _messageService.ShowSaveFileDialogAsync("Select a location to save the .OBJ file", Path.GetFileNameWithoutExtension(ActiveFile.Path) + ".obj", "OBJ file|*.obj|All files|*.*");
        }
    }
}
