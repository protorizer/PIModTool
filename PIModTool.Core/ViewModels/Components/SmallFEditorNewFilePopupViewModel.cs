using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels.Components
{
    // Lightweight non MvX viewmodel for lightweight popup
    // Clunky for now, polish up after initial release
    public class SmallFEditorNewFilePopupViewModel: INotifyPropertyChanged
    {
        private string? _fileName;
        private FileType? _fileType;

        public string? FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public FileType? FileType
        {
            get => _fileType;
            set { _fileType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> FileTypes { get; } = new()
        {
            "EventScript",
            "ObjectScript",
            "PitaText",
            "PSCScript",
            "XML",
            "Unknown"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
