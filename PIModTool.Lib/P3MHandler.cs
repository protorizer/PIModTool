using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib
{
    public static class P3MHandler
    {
        private static byte[] CreateDDSHeader(int width, int height)
        {
            using MemoryStream header = new MemoryStream(128);
            using BinaryWriter writer = new BinaryWriter(header);

            // "DDS "
            writer.Write(new[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });

            // Size
            writer.Write(124);

            // Flags
            writer.Write(0x0002100F);

            // Height & Width
            writer.Write(height);
            writer.Write(width);

            // Pitch or Linear Size (for DXT1: max(1, ((width+3)/4)) * 8)
            int linearSize = Math.Max(1, ((width + 3) / 4)) * 8 * ((height + 3) / 4);
            writer.Write(linearSize);

            // Depth
            writer.Write(0);

            // MipMap count
            writer.Write(0);

            // Reserved (11 ints)
            for (int i = 0; i < 11; i++)
            {
                writer.Write(0);
            }

            // Pixel format (DDS_PIXELFORMAT)
            writer.Write(32);                // size
            writer.Write(0x00000004);        // flags (FOURCC)
            writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'1' }); // FOURCC
            writer.Write(0); // RGBBitCount
            writer.Write(0); // RBitMask
            writer.Write(0); // GBitMask
            writer.Write(0); // BBitMask
            writer.Write(0); // ABitMask

            // Caps
            writer.Write(0x1000);

            // Caps2, Caps3, Caps4
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Reserved2
            writer.Write(0);

            return header.ToArray();
        }

        public static byte[] ConvertToDDS(byte[] p3m)
        {
            int width = BitConverter.ToInt16(p3m, 0);
            int height = BitConverter.ToInt16(p3m, 2);

            byte[] header = CreateDDSHeader(width, height);
            byte[] result = new byte[header.Length + p3m.Length - 32];

            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(p3m, 32, result, header.Length, p3m.Length - 32);

            return result;
        }
    }
}
