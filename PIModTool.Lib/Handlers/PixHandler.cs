using PIModTool.Lib.Extensions;
using PIModTool.Lib.Types;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

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

        // Attempts to zlib-compress a binary chunk
        private static byte[]? TryCompressZlib(byte[] data)
        {
            try
            {
                using MemoryStream dataStream = new MemoryStream(data);
                using ZLibStream deflateStream = new ZLibStream(dataStream, CompressionMode.Compress);
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

                            //Debug.WriteLine(zSize);

                            if (zSize == 0) break;

                            pixFile.Position = (pixFile.Position + 0x7FF) & ~0x7FFL; // Align to a 0x800 padded boundary

                            byte[] zlibData = reader.ReadBytes(zSize);
                            byte[]? decompressedData = TryDecompressZlib(zlibData);

                            if (decompressedData == null) // Bad chunk
                            {
                                throw new Exception("Bad zlib chunk");
                            }

                            using MemoryStream dataStream = new MemoryStream(decompressedData);
                            using (BinaryReader chunkReader = new BinaryReader(dataStream))
                            {
                                int numFiles = chunkReader.ReadInt32();
                                for (int i = 0; i < numFiles; i++)
                                {
                                    int fileSize = chunkReader.ReadInt32();

                                    // Corrupted streams sometimes show up in prototypes liek DCAP - unsure if it's a difference in file format or if the builds are corrupted
                                    if (fileSize <= 0 || fileSize > (dataStream.Length - dataStream.Position))
                                    {
                                        Debug.WriteLine("Skipping corrupted or empty stream");
                                        break;
                                    }
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

        // Writes a .pix file
        public static async Task WritePix(List<GenericFile> data, string fileName)
        {
            try
            {
                using FileStream pixFile = File.OpenWrite(fileName);
                using BinaryWriter pixWriter = new BinaryWriter(pixFile);

                foreach(GenericFile file in data)
                {
                    // Prepare data packet (header + data)
                    using MemoryStream fileStream = new MemoryStream();
                    using BinaryWriter fileWriter = new BinaryWriter(fileStream);
                    fileWriter.Write(1); // Num files in chunk = 1
                    fileWriter.Write(file.Data.Length); // fileSize
                    fileWriter.Write(Encoding.UTF8.GetBytes(file.Path)); // path
                    fileWriter.Write('\0'); // null terminator
                    fileWriter.Write(file.Data); // Actual data

                    // Zlib compress data
                    byte[]? compressedData = TryCompressZlib(fileStream.ToArray());
                    if (compressedData == null)
                    {
                        throw new Exception("Failed to compress file");
                    }

                    // Write zSize, size
                    pixWriter.Write(compressedData.Length);
                    pixWriter.Write(fileStream.Length);

                    // Pad to 0x800 padded boundaries
                    long dataOffset = (pixFile.Position + 0x7FF) & ~0x7FFL;
                    long numPadding = dataOffset - pixFile.Position;
                    for (int i = 0; i < numPadding; i++)
                    {
                        pixWriter.Write('\0');
                    }

                    // Write data
                    pixWriter.Write(compressedData);
                }

                // Write empty entry to denote EOF
                pixWriter.Write(0); // Empty zSize
                pixWriter.Write(0); // Empty size
            }
            catch(Exception e)
            {
                Debug.Fail(e.Message);
                return;
            }
        }
    }
}
