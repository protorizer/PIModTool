//using PIModTool.Lib.Types;
//using System;
//using System.Buffers.Binary;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace PIModTool.Lib
//{
//    public static class X2MHandler
//    {
//        private static byte[] CreateDDSHeader(int width, int height)
//        {
//            using MemoryStream header = new MemoryStream(128);
//            using BinaryWriter writer = new BinaryWriter(header);

//            // "DDS "
//            writer.Write(new[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });

//            // Size
//            writer.Write(124);

//            // Flags
//            writer.Write(0x00081007);
//            //writer.Write(0x0002100F);

//            // Height & Width
//            writer.Write(height);
//            writer.Write(width);

//            // Pitch or Linear Size (for DXT1: max(1, ((width+3)/4)) * 8)
//            int linearSize = Math.Max(1, ((width + 3) / 4)) * 8 * ((height + 3) / 4);
//            writer.Write(linearSize);

//            // Depth
//            writer.Write(0);

//            // MipMap count
//            writer.Write(0);

//            // Reserved (11 ints)
//            for (int i = 0; i < 11; i++)
//            {
//                writer.Write(0);
//            }

//            // Pixel format (DDS_PIXELFORMAT)
//            writer.Write(32);                // size
//            writer.Write(0x00000004);        // flags (FOURCC)
//            writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'1' }); // FOURCC
//            writer.Write(0); // RGBBitCount
//            writer.Write(0); // RBitMask
//            writer.Write(0); // GBitMask
//            writer.Write(0); // BBitMask
//            writer.Write(0); // ABitMask

//            // Caps
//            writer.Write(0x1000);

//            // Caps2, Caps3, Caps4
//            writer.Write(0);
//            writer.Write(0);
//            writer.Write(0);

//            // Reserved2
//            writer.Write(0);

//            return header.ToArray();
//        }

//        public static void SwapEndian(byte[] data)
//        {
//            Span<ushort> values = MemoryMarshal.Cast<byte, ushort>(data);
//            for(int i = 0; i < values.Length; i++)
//            {
//                values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
//            }
//        }

//        // Inserts a 0 between each bit, allowing for x and y Morton bits to be interleaved
//        private static uint SpreadBits(uint n)
//        {
//            n = (n | (n << 8)) & 0x00FF00FF;
//            n = (n | (n << 4)) & 0x0F0F0F0F;
//            n = (n | (n << 2)) & 0x33333333;
//            n = (n | (n << 1)) & 0x55555555;
//            return n;
//        }

//        // X360 util: compute the Morton swizzling index of a block
//        private static uint MortonIndex(uint x, uint y)
//        {
//            return SpreadBits(x) | (SpreadBits(y) << 1);
//        }

//        // X360 util: Convert Morton swizzled texture to linear space
//        // Block size is 8 for DXT1, 16 otherwise
//        public static byte[] Unswizzle(byte[] data, int width, int height, int blockSize)
//        {
//            // DXT formats pack 4x4 pixel tiles into one compressed block
//            int blocksWide = Math.Max(1, width / 4);
//            int blocksTall = Math.Max(1, height / 4);

//            byte[] result = new byte[blocksWide * blocksTall * blockSize];

//            for (int y = 0; y < blocksTall; y++)
//            {
//                for (int x = 0; x < blocksWide; x++)
//                {
//                    // The Xbox 360 stores blocks at the Morton-order offset of their (x, y) position
//                    // We read from that swizzled location and write to the linear destination
//                    int srcOffset = (int)MortonIndex((uint)x, (uint)y) * blockSize;
//                    int dstOffset = (y * blocksWide + x) * blockSize;

//                    if (srcOffset + blockSize <= data.Length &&
//                        dstOffset + blockSize <= result.Length)
//                    {
//                        Buffer.BlockCopy(data, srcOffset, result, dstOffset, blockSize);
//                    }
//                }
//            }

//            return result;
//        }

//        public static byte[] ConvertToDDS(byte[] p3m)
//        {
//            int width = BitConverter.ToInt16(p3m, 0);
//            int height = BitConverter.ToInt16(p3m, 2);

//            int format = p3m[16];

//            int length = BitConverter.ToInt32(p3m, 20);

//            byte[] header = CreateDDSHeader(width, height);
//            byte[] data = new byte[length];
//            Buffer.BlockCopy(p3m, 32, data, 0, length);
//            SwapEndian(data); // Change from big to little endian
//            data = Xbox360TextureUntile.Untile360Dxt(data, width, height, 8);
//            File.WriteAllBytes("D:/Hacker Stuff/Pseudo Interactive/PIModTool Testing/pix/textures/pimod_data_fin", data);

//            byte[] result = new byte[header.Length + data.Length];

//            Buffer.BlockCopy(header, 0, result, 0, header.Length);
//            Buffer.BlockCopy(data, 0, result, header.Length, data.Length);

//            return result;
//        }

//        // Extract the corresponding HD texture from textures.pit
//        public static byte[] GetHighResTexture(byte[] p3m, byte[] pit)
//        {
//            int hdWidth = BitConverter.ToInt16(p3m, 4);
//            int hdHeight = BitConverter.ToInt16(p3m, 6);

//            int hdFileSize = BitConverter.ToInt32(p3m, 24);
//            int hdFileOffset = BitConverter.ToInt32(p3m, 28);

//            byte[] header = CreateDDSHeader(hdWidth, hdHeight);
//            byte[] result = new byte[header.Length +  hdFileSize];

//            Buffer.BlockCopy(header, 0, result, 0, header.Length);
//            Buffer.BlockCopy(pit, hdFileOffset, result, header.Length, hdFileSize);

//            return result;
//        }
//    }
//}
