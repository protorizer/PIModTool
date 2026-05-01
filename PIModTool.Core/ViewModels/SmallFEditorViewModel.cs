using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Search;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PIModTool.Core.Types;
using PIModTool.Lib;
using PIModTool.Lib.Types;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace PIModTool.Core.ViewModels
{
    public class SmallFEditorViewModel : MvxViewModel<SmallFEditorParams>
    {
        private string _fileName = "SmallF.dat";
		private ObservableCollection<SmallFFile> _files = new ObservableCollection<SmallFFile>();
        private SmallFFile? _activeFile;
        private FileType _activeFileType;
        private bool _isImageFile;
        private byte[] _imageData;
        private readonly Dictionary<SmallFFile, TextDocument> _fileDocuments = new Dictionary<SmallFFile, TextDocument>();
        private TextDocument _editorDocument = new TextDocument();

        private ObservableCollection<SyntaxError> _syntaxErrors = new ObservableCollection<SyntaxError>();
        private readonly IMessageService _messageService;
        private SearchPanel? _searchPanel;
        public IMvxCommand CreateNewFileCommand => new MvxAsyncCommand(CreateNewFileAsync);

        public IMvxCommand ExportFileCommand => new MvxAsyncCommand<SmallFFile>(ExportIndividualFileAsync);

        public IMvxNavigationService _navigationService;

        public SmallFEditorViewModel(IMessageService messageService, IMvxNavigationService navigationService)
        {
            _messageService = messageService;
            _navigationService = navigationService;
        }

		public ObservableCollection<SmallFFile> Files
		{
			get { return _files; }
			set { 
                SetProperty(ref _files, value);
            }
		}

        public string FileName
        {
            get { return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }

        public SmallFFile? ActiveFile
        {
            get { return _activeFile; }
            set
            {
                SaveDocumentToFile(_activeFile);
                SetProperty(ref _activeFile, value);
                LoadFileIntoDocument(_activeFile);
            }
        }

        public FileType ActiveFileType
        {
            get { return _activeFileType; }
            set { SetProperty(ref _activeFileType, value); }
        }

        public bool IsImageFile
        {
            get { return _isImageFile; }
            set { SetProperty(ref _isImageFile, value); }
        }

        public byte[] ImageData
        {
            get { return _imageData; }
            set { SetProperty(ref _imageData, value); }
        }

        public TextDocument EditorDocument
        {
            get { return _editorDocument; }
            set { SetProperty(ref _editorDocument, value); }
        }

        public ObservableCollection<SyntaxError> SyntaxErrors
        {
            get { return _syntaxErrors; }
            set { SetProperty(ref _syntaxErrors, value); }
        }

        public override void Prepare(SmallFEditorParams param)
        {
            Files = new ObservableCollection<SmallFFile>(param.Files);
            base.Prepare();
        }

        public override Task Initialize()
        {
            EditorDocument.Text = "Select a file on the left panel to edit it.";
            return base.Initialize();
        }

        private void LoadFileIntoDocument(SmallFFile? file)
        {
            IsImageFile = false;
            if (file == null)
            {
                EditorDocument = new TextDocument();
                return;
            }
            ActiveFileType = file.Type;

            switch (ActiveFileType) {
                case FileType.DDS:
                    IsImageFile = true;
                    ImageData = file.Data;
                    break;
                case FileType.UnknownBinary:
                    ActiveFile = null;
                    EditorDocument.Text = "File is a binary file with no available editor.";
                    break;
                case FileType.Unknown:
                    ActiveFile = null;
                    EditorDocument.Text = "PIModTool was unable to determine the filetype. Please contact the developer protorizer.";
                    break;
                default:
                    if (!_fileDocuments.TryGetValue(file, out TextDocument fileDoc))
                    {
                        fileDoc = new TextDocument(Encoding.UTF8.GetString(file.Data));
                        _fileDocuments[file] = fileDoc;
                    }

                    EditorDocument = fileDoc;
                    break;
            }

        }

        // Doesn't save to disk, writes the active text editor's contents to the SmallFFile struct's byte[]
        public void SaveDocumentToFile(SmallFFile? file)
        {
            if (file == null)
            {
                return;
            }

            if(_fileDocuments.TryGetValue(file, out TextDocument fileDoc))
            {
                file.Data = Encoding.UTF8.GetBytes(fileDoc.Text);
            }
        }

        public async Task CreateNewFileAsync()
        {
            (bool confirmed, string? fileName, FileType? type) newFile = await _messageService.ShowNewFileDialogAsync();

            if(!newFile.confirmed) { return; }

            // Add new file
            string fileName = newFile.fileName!;
            FileType fileType = (FileType)newFile.type!;

            // Create new SmallFFile entry
            // Offset is solely used for reading from SmallFs not writing, so can safely be 0 here
            SmallFFile smallFFile = new SmallFFile(0, fileName, fileType);

            // Add new entry to ObservableCollection
            Files.Add(smallFFile);
        }

        public void ValidateSyntax()
        {
            SyntaxErrors.Clear();
            if (ActiveFile == null) {
                return; 
            }

            List<SyntaxError> errors = Lib.SyntaxValidation.Validate(EditorDocument.Text, ActiveFileType);

            foreach (var error in errors)
            {
                SyntaxErrors.Add(error);
            }
        }

        public CodeFormattingAction GetCodeFormattingAction(string currentLine, string input)
        {
            return CodeFormatting.ProcessInput(currentLine, input, ActiveFileType);
        }

        //
        // Saving / Exporting Handlers
        //

        private async Task ExportIndividualFileAsync(SmallFFile? file)
        {
            if(file == null) { 
                return; 
            }

            string extension = Path.GetExtension(file.Path);
            string? filePath;

            if (!string.IsNullOrEmpty(extension))
            {
                filePath = await _messageService.ShowSaveFileDialogAsync("Select location to export file", Path.GetFileName(file.Path), extension + " file|*" + extension + "|" + "All files|*.*");
            }
            else
            {
                filePath = await _messageService.ShowSaveFileDialogAsync("Select location to export file", Path.GetFileName(file.Path), "All files|*.*");
            }

            if(filePath == null) {  return; }

            GenericHandler.SaveFile(file, Path.GetDirectoryName(filePath)!);

            // TODO: In future, add error handling and a confirmation popup
        }

        public void ExportAllFiles(string folderPath)
        {
            GenericHandler.SaveFiles(Files.ToList(), folderPath);

            // TODO: In future, add error handling and a confirmation popup
        }

        public void SaveSmallF(string filePath)
        {
            SmallFHandler.SaveSmallF(Files.ToList(), filePath);

            // TODO: In future, add error handling and a confirmation popup
        }

        public async Task ChangeView<TViewModel>() where TViewModel : MvxViewModel
        {
            await _navigationService.Navigate<TViewModel>();
        }
    }
}
