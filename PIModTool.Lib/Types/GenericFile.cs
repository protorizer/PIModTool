using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib.Types
{
    public class GenericFile
    {
        private string _path = "";
        private FileType _type = FileType.Unknown;
        private byte[] _data = [];

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        public FileType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public GenericFile() {
            _path = "";
            _data = Array.Empty<byte>();
        }
        public GenericFile(string path, byte[] data)
        {
            _path = path;
            _data = data;
        }
    }
}
