using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Core.Types
{
    public class SmallFEditorParams
    {
        private string _fileName = "SmallF.dat";
        private List<SmallFFile> _files = new List<SmallFFile>();

        public string FileName { 
            get { return _fileName; } 
            set { _fileName = value; }
        }

        public List<SmallFFile> Files
        {
            get { return _files; }
            set { _files = value; }
        }
    }
}
