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

        // Locate the vertex section of the model file and returns the position of the first vertex and number of vertices
        // TODO: This function uses heuristic analysis to roughly locate the vertices. Figure out exactly how to locate the vertex section
        // And reimplement this with higher accuracy
        // TODO: Make this work with X360 DRPs
        public static (int vertexOffset, int vertexCount) FindVertexInfo(byte[] modelData)
        {
            ReadOnlySpan<byte> modelSpan = new ReadOnlySpan<byte>(modelData);

            // Heuristic logic: Find a section with a valid little endian int32 followed immediately by 3 plausible big endian float32s
            // Then, make sure we have plausible float32s at least 4 more times with 16 bytes padding between them
            // This means objects with less than 5 vertices won't be detected, but that shouldn't be much of an issue for now
            // Stop 144 bytes before end of data because 5 vertices + the header = 144 bytes
            for(int i = 0; i < modelSpan.Length - 144; i++)
            {
                // Check if a valid positive LE int32 is present here
                int testNum = BinaryPrimitives.ReadInt32LittleEndian(modelSpan.Slice(i, 4));
                // Implausible numbers - PS3 game probably won't have over a hundred thousand vertices
                if(testNum <= 0 || testNum > 100000)
                {
                    continue;
                }

                int vertexOffset = i + 4;
                bool flagInvalid = false;
                for (int vertexNum = 0; vertexNum < 5; vertexNum++)
                {
                    // Check if 3 valid BE float32s are present here
                    float[] testFloats =
                    [
                        BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset, 4)),
                        BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset + 4, 4)),
                        BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset + 8, 4)),
                    ];

                    for (int j = 0; j < testFloats.Length; j++)
                    {
                        string floatStr = testFloats[j].ToString(CultureInfo.InvariantCulture);
                        // Implausible numbers - Of a scale that contains E in it
                        if (!float.IsNormal(testFloats[j]) || floatStr.Contains("E"))
                        {
                            flagInvalid = true;
                            break;
                        }
                    }

                    if (flagInvalid)
                    {
                        break;
                    }

                    vertexOffset = vertexOffset + 28;
                }

                if (!flagInvalid)
                {
                    return (i, testNum);
                }
            }

            return (0, 0);
        }

        // Get vertex data from a DRP model file
        public static (List<MeshVertex>?, int sectionEnd) GetVertexData(GenericFile modelFile)
        {
            if(modelFile.Type != FileType.DRP)
            {
                return (null, -1);
            }

            //string tmp = Path.Combine(Path.GetTempPath(), "PIModTool", Guid.NewGuid().ToString(), modelFile.Path);
            //Directory.CreateDirectory(Path.GetDirectoryName(tmp));
            //File.WriteAllBytes(tmp, modelFile.Data);

            try
            {
                List<MeshVertex> vertices = new List<MeshVertex>();

                int vertexOffset;
                int vertexCount;
                (vertexOffset, vertexCount) = FindVertexInfo(modelFile.Data);

                Debug.WriteLine("Found " + vertexCount + " vertices at offset " + vertexOffset);
                vertexOffset += 4;

                // Populate list of vertices
                ReadOnlySpan<byte> modelSpan = new ReadOnlySpan<byte>(modelFile.Data);
                for (int i = 0; i < vertexCount; i++)
                {
                    float xPos = BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset, 4));
                    float yPos = BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset + 4, 4));
                    float zPos = BinaryPrimitives.ReadSingleBigEndian(modelSpan.Slice(vertexOffset + 8, 4));
                    vertices.Add(new MeshVertex(xPos, yPos, zPos));
                    vertexOffset += 28;
                }

                return (vertices, vertexOffset);
            }
            catch
            {
                return (null, -1);
            }
        }

        // Locates a face section in the DRP file
        // TODO: This function uses heuristic analysis to roughly locate the vertices. Figure out exactly how to locate the vertex section
        // And reimplement this with higher accuracy
        public static int FindFaceInfo(ReadOnlySpan<byte> modelData, int numVertices)
        {
            // Heuristic logic: Scan file starting at offset for a large number of contiguous BE shorts that are between 0 and numVertices
            // Section must start with a 0, since all face sections start at 0 index
            // Then continue reading them and creating faces until we read an invalid short
            // Then return
            // TODO: continue scanning for additional facee sections until we run out

            for (int i = 0; i < modelData.Length; i++)
            {
                // Heuristic number is arbitrary, adjust if getting false positives
                bool invalid = false;
                for (int j = 0; j < 50; j++)
                {
                    if(i + (j * 2) > modelData.Length - 10)
                    {
                        return -1;
                    }
                    int testIndex = BinaryPrimitives.ReadInt16BigEndian(modelData.Slice(i + (j * 2)));
                    if (testIndex < 0 || testIndex > numVertices || (j == 0 && testIndex != 0)) // Invalid index
                    {
                        invalid = true;
                        break;
                    }
                }
                if (!invalid)
                {
                    return i;
                }
            }

            return -1;
        }

        // Get face data from a DRP model file
        public static List<MeshFace>? GetFaceData(GenericFile modelFile, int offset, int numVertices)
        {
            if (modelFile.Type != FileType.DRP)
            {
                return null;
            }

            ReadOnlySpan<byte> modelSpan = new ReadOnlySpan<byte>(modelFile.Data);
            modelSpan = modelSpan.Slice(offset);

            int faceSectionStart = FindFaceInfo(modelSpan, numVertices);

            if (faceSectionStart < 0)
            {
                return null;
            }

            List<MeshFace> triangles = new List<MeshFace>();
            int byteOffset = 0;

            // Iterate through indices and create triangles, until we find an invalid index
            // Indices are stored in triangle strip form, but HelixToolkit doesn't support this - have to convert to triangles
            // Triangle strip shares 2 vertices with the previous triangle - store in queue for easy management
            // Read 3 vertices in and make a triangle, then continuously pop the queue and push a new element, turning the contents into a triangle
            Queue<int> indices = new Queue<int>();
            for (int i = 0; i < 3; i++)
            {
                indices.Enqueue(BinaryPrimitives.ReadInt16BigEndian(modelSpan.Slice(faceSectionStart + byteOffset)));
                byteOffset += 2;
            }

            int[] tmp = indices.ToArray();
            triangles.Add(new MeshFace([tmp[0], tmp[1], tmp[2]]));

            while (true)
            {
                // Pop last added vertex
                indices.Dequeue();
                // Read a new vertex
                int num = BinaryPrimitives.ReadInt16BigEndian(modelSpan.Slice(faceSectionStart + byteOffset));
                // Check if the vertex is valid
                if (num < 0 || num > numVertices)
                {
                    break;
                }
                indices.Enqueue(num);
                // Add the current indices as a triangle
                tmp = indices.ToArray();
                triangles.Add(new MeshFace([tmp[0], tmp[1], tmp[2]]));
                // Increment offset
                byteOffset += 2;
            }

            Debug.WriteLine("Found " + triangles.Count + " faces starting at offset " + faceSectionStart);

            return triangles;
        }

        //public static List<MeshFace>? GetFaceData(GenericFile modelFile, int offset, int numVertices)
        //{
        //    if (modelFile.Type != FileType.DRP)
        //    {
        //        return null;
        //    }

        //    ReadOnlySpan<byte> modelSpan = new ReadOnlySpan<byte>(modelFile.Data);
        //    modelSpan = modelSpan.Slice(offset);
        //    List<MeshFace> triangles = new List<MeshFace>();

        //    int faceSectionStart = FindFaceInfo(modelSpan, numVertices);

        //    if (faceSectionStart < 0)
        //    {
        //        return null;
        //    }
        //    int byteOffset = 0;

        //    int numPasses = 1;
        //    int maxPasses = 2;
        //    while (true)
        //    {
        //        if(numPasses > maxPasses) { return triangles; }
        //        int numFoundFaces = 0;
        //        // Iterate through indices and create triangles, until we find an invalid index
        //        // Indices are stored in triangle strip form, but HelixToolkit doesn't support this - have to convert to triangles
        //        // Triangle strip shares 2 vertices with the previous triangle - store in queue for easy management
        //        // Read 3 vertices in and make a triangle, then continuously pop the queue and push a new element, turning the contents into a triangle
        //        Queue<int> indices = new Queue<int>();
        //        for (int i = 0; i < 3; i++)
        //        {
        //            indices.Enqueue(BinaryPrimitives.ReadInt16BigEndian(modelSpan.Slice(faceSectionStart + byteOffset)));
        //            byteOffset += 2;
        //        }

        //        int[] tmp = indices.ToArray();
        //        triangles.Add(new MeshFace([tmp[0], tmp[1], tmp[2]]));

        //        while (true)
        //        {
        //            // Pop last added vertex
        //            indices.Dequeue();

        //            int num;
        //            // Read a new vertex
        //            num = BinaryPrimitives.ReadInt16BigEndian(modelSpan.Slice(faceSectionStart + byteOffset));

        //            // Check if the vertex is valid
        //            if (num < 0 || num > numVertices)
        //            {
        //                break;
        //            }
        //            indices.Enqueue(num);
        //            // Add the current indices as a triangle
        //            tmp = indices.ToArray();
        //            triangles.Add(new MeshFace([tmp[0], tmp[1], tmp[2]]));
        //            // Increment offset
        //            byteOffset += 2;
        //            numFoundFaces++;
        //        }

        //        Debug.WriteLine("PASS " + numPasses + ": Found " + numFoundFaces + " faces starting at offset " + faceSectionStart);

        //        // Second pass test
        //        modelSpan = modelSpan.Slice(faceSectionStart + byteOffset);
        //        faceSectionStart = FindFaceInfo(modelSpan, numVertices);
        //        byteOffset = 0;

        //        if (faceSectionStart < 0)
        //        {
        //            return triangles;
        //        }

        //        if(numPasses == 1)
        //        {
        //            triangles.Clear();
        //        }

        //        numPasses++;
        //    }
    //  }
    }
}
