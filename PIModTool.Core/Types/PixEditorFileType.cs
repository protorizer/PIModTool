using PIModTool.Core.ViewModels.PixEditorSubviews;
using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.Types
{
    // Type used for the pix editor ComboBox to select a subview
    public class PixEditorFileType
    {
        public string DisplayName { get; }
        public FileType FileType { get; }
        public IPixEditorSubviewViewModel SubViewModel { get; }

        public PixEditorFileType(string displayName, FileType fileType, IPixEditorSubviewViewModel subViewModel)
        {
            DisplayName = displayName;
            FileType = fileType;
            SubViewModel = subViewModel;
        }
    }
}
