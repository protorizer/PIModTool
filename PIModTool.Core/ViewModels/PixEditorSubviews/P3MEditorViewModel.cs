using MvvmCross.ViewModels;
using PIModTool.Lib.Types;

namespace PIModTool.Core.ViewModels.PixEditorSubviews
{
    public class P3MEditorViewModel : MvxViewModel, IPixEditorSubviewViewModel
    {
        private GenericFile? _activeFile;
        private byte[]? _activeTexture;

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

        public void SetActiveFile(GenericFile? file)
        {
            ActiveFile = file;
        }

        public Task ExportDataAsync()
        {
            return null;
        }

        public void LoadTexture()
        {
            if (ActiveFile == null)
            {
                ActiveTexture = null;
                return;
            }
            ActiveTexture = Lib.P3MHandler.ConvertToDDS(ActiveFile.Data);
        }
    }
}
