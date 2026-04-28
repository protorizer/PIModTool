using MvvmCross.Commands;
using MvvmCross.ViewModels;
using PIModTool.Lib.Types;
using PIModTool.Lib;

namespace PIModTool.Core.ViewModels.PixEditorSubviews
{
    public class TextureEditorViewModel : MvxViewModel, IPixEditorSubviewViewModel
    {
        private readonly IMessageService _messageService;
        private GenericFile? _activeFile;
        private byte[]? _activeTexture;
        private byte[]? _activeTextureHD;
        private int _selectedTab;
        private byte[]? _pitFile;

        public IMvxCommand OpenPitCommand => new MvxAsyncCommand(OpenPit);

        public TextureEditorViewModel(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public GenericFile? ActiveFile
        {
            get {  return _activeFile; }
            set
            {
                SetProperty(ref _activeFile, value);
                LoadTexture();
            }
        }

        public byte[]? ActiveTexture
        {
            get { return _activeTexture; }
            set { SetProperty(ref _activeTexture, value); }
        }

        public byte[]? ActiveTextureHD
        {
            get { return _activeTextureHD; }
            set { SetProperty(ref _activeTextureHD, value); }
        }

        public int SelectedTab
        {
            get { return _selectedTab; }
            set { SetProperty(ref _selectedTab, value); }
        }

        public bool HDTexture
        {
            get { return SelectedTab == 1; }
        }

        public byte[]? PitFile
        {
            get { return _pitFile; }
            set { SetProperty(ref _pitFile, value); }
        }

        public bool LoadedPit
        {
            get { return PitFile != null; }
        }

        public void SetActiveFile(GenericFile? file)
        {
            ActiveFile = file;
        }

        public async Task ExportDataAsync()
        {
            if (HDTexture)
            {
                if(ActiveTextureHD == null)
                {
                    await _messageService.ShowNotifAsync("No HD texture selected to export.");
                    return;
                }
                string? filePath = await _messageService.ShowSaveFileDialogAsync("Save as DDS file", Path.GetFileNameWithoutExtension(ActiveFile.Path) + ".dds", "DDS file|*.dds|All files|*.*");
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                File.WriteAllBytes(filePath, ActiveTextureHD);
                await _messageService.ShowNotifAsync($"Texture exported successfully to {Path.GetFileName(filePath)}");
            }
            else
            {
                if (ActiveTexture == null)
                {
                    await _messageService.ShowNotifAsync("No texture selected to export.");
                    return;
                }

                string? filePath = await _messageService.ShowSaveFileDialogAsync("Save as DDS file", Path.GetFileNameWithoutExtension(ActiveFile.Path) + ".dds", "DDS file|*.dds|All files|*.*");
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                File.WriteAllBytes(filePath, ActiveTexture);
                await _messageService.ShowNotifAsync($"Texture exported successfully to {Path.GetFileName(filePath)}");
            }
        }

        private async Task OpenPit()
        {
            string? filePath = await _messageService.ShowOpenFileDialogAsync("Select textures.pit", "textures.pit|textures.pit|.pit file|*.pit|All files|*.*");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            PitFile = File.ReadAllBytes(filePath);

            LoadTexture();

            await RaisePropertyChanged(() => LoadedPit);
        }

        public void LoadTexture()
        {
            if (ActiveFile == null)
            {
                ActiveTexture = null;
                return;
            }
            ActiveTexture = TextureHandler.ConvertToDDS(ActiveFile.Data);

            if (LoadedPit)
            {
                ActiveTextureHD = TextureHandler.ConvertToDDS(ActiveFile.Data, true, PitFile);
            }
        }
    }
}
