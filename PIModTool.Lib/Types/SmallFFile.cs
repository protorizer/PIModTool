using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib.Types
{
    public class SmallFFile: GenericFile
    {
        private int _offset;

        public int Offset { 
            get { return _offset; } 
            set { _offset = value; }
        }

        public SmallFFile(int offset, string path, FileType type)
        {
            _offset = offset;
            Path = path;
            Type = type;
        }
    }
}
