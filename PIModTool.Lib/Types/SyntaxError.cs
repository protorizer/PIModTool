using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib.Types
{
    public class SyntaxError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; } = 1;
        public string Message { get; set; }
    }
}
