
using MvvmCross.ViewModels;
using PIModTool.Lib.Types;
using System.Collections.ObjectModel;


namespace PIModTool.Core.ViewModels.Components
{
    public sealed class FileTreeNodeViewModel: MvxNotifyPropertyChanged
    {
        private string _name = string.Empty;
        private bool _isExpanded = true;
        private bool _isRenaming;
        private bool _isSelected;

        public bool IsFolder { get; }
        public ObservableCollection<FileTreeNodeViewModel> Children { get; } = new ObservableCollection<FileTreeNodeViewModel>();

        public string Name
        {
            get => _name;
            set { SetProperty(ref _name, value); }
        }
        public bool IsExpanded
        {
            get => _isExpanded;
            set { SetProperty(ref _isExpanded, value); }
        }
        public bool IsRenaming
        {
            get => _isRenaming;
            set { SetProperty(ref _isRenaming, value); ; }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set { SetProperty(ref _isSelected, value); }
        }

        public GenericFile? File { get; }
        public string FolderPath { get; private set; } = string.Empty;

        public FileTreeNodeViewModel(GenericFile file)
        {
            IsFolder = false;
            File = file;
            _name = Path.GetFileName(file.Path.Replace('\\', '/'));
        }

        public FileTreeNodeViewModel(string folderPath)
        {
            IsFolder = true;
            FolderPath = folderPath;
            _name = folderPath.Contains('/') ? folderPath[(folderPath.LastIndexOf('/') + 1)..] : folderPath;
        }

        public void SetFolderPath(string newPath)
        {
            FolderPath = newPath;
            Name = newPath.Contains('/') ? newPath[(newPath.LastIndexOf('/') + 1)..] : newPath;
        }
    }
}
