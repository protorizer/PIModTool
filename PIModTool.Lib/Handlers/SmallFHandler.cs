using PIModTool.Lib.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace PIModTool.Lib
{
    public static class SmallFHandler
    {
        // Helper function: Makes list of files and their information
        private static List<SmallFFile>? ReadMetadata(FileStream file)
        {
            List<SmallFFile> files = new List<SmallFFile>();
            BinaryReader bin = new BinaryReader(file);
            int offset;
            int pathLength;
            string path;

            while (true)
            {
                offset = bin.ReadInt32(); // Offset is 4 byte integer
                if(offset > file.Length) // Check if the offset is invalid
                {
                    return null;
                }
                pathLength = bin.ReadByte(); // Path length is 1 byte integer
                path = new string(bin.ReadChars(pathLength)); // File path is string of length pathLength
                if(path.IndexOfAny(Path.GetInvalidPathChars()) >= 0){ // Check if path is invalid
                    return null;
                }

                bin.ReadByte(); // Advance past the null character
                files.Add(new SmallFFile(offset, path, FileType.Unknown));
                if (pathLength == 0) // Denotes the end of the header
                {
                    break;
                }
            }

            if(files.Count < 2) // Check if nothing was read
            {
                return null;
            }

            return files;
        }

        // Helper function: Populates list of files with their binary contents
        private static void ReadFileData(FileStream smallF, ref List<SmallFFile> files)
        {
            for(int i = 0; i < files.Count - 1; i++) { // Final entry is just to denote end point of the last file
                int size = files[i+1].Offset - files[i].Offset;
                byte[] buffer = new byte[size];

                smallF.Seek(files[i].Offset, SeekOrigin.Begin);
                smallF.Read(buffer, 0, size);
                if (Path.GetExtension(files[i].Path) == ".psc")
                {
                    files[i].Type = FileType.PSCScript;
                }
                else {
                    files[i].Type = FileTypeHandler.DetectFileType(buffer);
                }
                files[i].Data = buffer;
            }

            // Remove last element that isn't actually a file
            files.RemoveAt(files.Count - 1);
        }

        // Opens SmallF.dat & returns array of its contents. Assumes path is valid.
        public static List<SmallFFile>? ReadSmallF(string filePath)
        {
            FileStream file = File.OpenRead(filePath);
            List<SmallFFile>? contents = ReadMetadata(file);
            if(contents == null)
            {
                return null;
            }
            ReadFileData(file, ref contents);

            file.Close();
            return contents;
        }

        // Calculate size of header (added to offset): 4 byte offset + 1 byte path length + path + null byte
        private static int CalcHeaderSize<T>(List<T> files) where T: GenericFile
        {
            int headerSize = 6; // 6 bytes for the end of the header (the null entry)
            foreach (T file in files)
            {
                headerSize += 6 + file.Path.Length;
            }
            return headerSize;
        }
        public static void SaveSmallF<T>(List<T> files, string savePath) where T : GenericFile
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            int offset = CalcHeaderSize<T>(files);
            
            // Write the header
            foreach (T file in files)
            {
                writer.Write(offset);
                writer.Write((byte)file.Path.Length);
                writer.Write(Encoding.UTF8.GetBytes(file.Path));
                writer.Write('\0');
                offset += file.Data.Length;
            }

            // End of header
            writer.Write(offset);
            writer.Write('\0');
            writer.Write('\0');

            // Write file contents
            foreach (T file in files)
            {
                writer.Write(file.Data);
            }

            // Write null block. Number of null bytes is arbitrary but I used the number from Full Auto 2's smallf.dat
            for(int i = 0; i < 742; i++)
            {
                writer.Write('\0');
            }

            GenericFile fileToWrite = new GenericFile(Path.GetFileName(savePath), stream.ToArray());
            writer.Dispose();
            stream.Dispose();

            string? folder = Path.GetDirectoryName(savePath);
            if(folder != null)
            {
                GenericHandler.SaveFile(fileToWrite, folder);
            }
        }
    }
}
