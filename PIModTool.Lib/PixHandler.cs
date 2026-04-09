using PIModTool.Lib.Extensions;
using PIModTool.Lib.Types;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace PIModTool.Lib
{
    public static class PixHandler
    {
        // Attempts to zlib-decompress a binary chunk
        private static byte[]? TryDecompressZlib(byte[] data)
        {
            try
            {
                using MemoryStream dataStream = new MemoryStream(data);
                using ZLibStream deflateStream = new ZLibStream(dataStream, CompressionMode.Decompress);
                using MemoryStream outputStream = new MemoryStream();

                deflateStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        // Opens a .pix file and zlib-decompresses its contents
        // Based on the QuickBMS script on reshax: https://reshax.com/topic/840-full-auto-xbox-360-rxx-pix-pit-xzp
        // TODO: Add descriptive errors
        public static async Task<List<GenericFile>?> ReadPix(string filePath)
        {
            List<GenericFile> contents = new List<GenericFile>();
            try
            {
                using(FileStream pixFile = File.OpenRead(filePath))
                {
                    using (BinaryReader reader = new BinaryReader(pixFile))
                    {
                        while (true)
                        {
                            int zSize = reader.ReadInt32();
                            int size = reader.ReadInt32();

                            if (zSize == 0) break;

                            pixFile.Position = (pixFile.Position + 0x7FF) & ~0x7FFL; // Align to a 0x800 padded boundary

                            byte[] zlibData = reader.ReadBytes(zSize);
                            byte[]? decompressedData = TryDecompressZlib(zlibData);

                            if (decompressedData == null) // Bad chunk
                            {
                                throw new Exception("Bad zlib chunk");
                            }

                            using (BinaryReader chunkReader = new BinaryReader(new MemoryStream(decompressedData)))
                            {
                                int numFiles = chunkReader.ReadInt32();
                                for (int i = 0; i < numFiles; i++)
                                {
                                    int fileSize = chunkReader.ReadInt32();
                                    string path = chunkReader.ReadNullTerminatedString();
                                    byte[] fileData = chunkReader.ReadBytes(fileSize);
                                    FileType type;
                                    switch (Path.GetExtension(path))
                                    {
                                        case ".drp":
                                            type = FileType.DRP;
                                            break;
                                        case ".x2m":
                                            type = FileType.X2M;
                                            break;
                                        case ".p3m":
                                            type = FileType.P3M;
                                            break;
                                        default:
                                            type = FileType.UnknownBinary;
                                            break;
                                    }

                                    GenericFile file = new GenericFile(path, fileData);
                                    file.Type = type;
                                    contents.Add(file);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Unknown error
                Debug.Fail(e.Message);
                return null;
            }
            Debug.WriteLine("Found " + contents.Count + " files");
            return contents;
        }
    }
}
