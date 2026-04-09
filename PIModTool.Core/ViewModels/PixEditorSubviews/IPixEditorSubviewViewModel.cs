using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.ViewModels.PixEditorSubviews
{
    // Interface to be implemented by all pix editor subviews - contains definitions that handle communication between main and subviews
    public interface IPixEditorSubviewViewModel
    {
        // PixEditorViewModel calls this to send data to a subview
        void SetActiveFile(GenericFile? file);

        // PixEditorViewModel calls this to tell the subview to export its data
        Task ExportDataAsync();
    }
}
