using PIModTool.Lib.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib
{
    public class GenericHandler
    {
        public static void SaveFile<T>(T file, string location) where T : GenericFile
        {
            string targetPath = Path.Combine(location, file.Path);
            string? targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.WriteAllBytes(targetPath, file.Data);
        }
        public static void SaveFiles<T>(List<T> files, string location) where T: GenericFile
        {
            foreach (GenericFile file in files)
            {
                SaveFile(file, location);
            }
        }

        public static List<GenericFile>? OpenAllFilesInFolder(string folderPath)
        {
            List<GenericFile> files = new List<GenericFile>();

            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                FileStream fileData = File.OpenRead(filePath);
                byte[] buffer = new byte[fileData.Length];
                fileData.Read(buffer, 0, buffer.Length);
                GenericFile file = new GenericFile(filePath.Replace(folderPath + '\\', ""), buffer);
                files.Add(file);
            }

            if(files.Count == 0)
            {
                return null;
            }

            return files;
        }

    }
}
