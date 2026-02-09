using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib.Types
{
    // Contains text to be placed before/after the cursor when doing a code format
    public class CodeFormattingAction
    {
        public string PreCursorText { get; set; }
        public string PostCursorText { get; set; }
    }
}
