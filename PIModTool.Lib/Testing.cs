#if IS_CONSOLE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIModTool.Lib.Types;

namespace PIModTool.Lib
{
    internal class Testing
    {
        static void Main()
        {
            List<SmallFFile> files = SmallFHandler.ReadSmallF("D:\\Emulators\\RPCS3\\games\\Full Auto 2 Battlelines [BLUS30009]\\PS3_GAME\\USRDIR\\smallf.dat");
            GenericHandler.SaveFiles(files, "D:\\Hacker Stuff\\Pseudo Interactive\\test\\");
            SmallFHandler.SaveSmallF(files, "D:\\Hacker Stuff\\Pseudo Interactive\\testrepack.dat");
        }
    }
}

#endif