using PIModTool.Lib.Types;
using PIModTool.Lib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib
{
    public static class TextureHandler
    {
        private enum DDSType
        {
            DXT1,
            ATI2,
            DXT5
        };

        private static byte[] CreateDDSHeader(int width, int height, DDSType type)
        {
            using MemoryStream header = new MemoryStream(128);
            using BinaryWriter writer = new BinaryWriter(header);

            // "DDS "
            writer.Write(new[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });

            // Size
            writer.Write(124);

            // Flags
            writer.Write(0x00021007);

            // Height & Width
            writer.Write(height);
            writer.Write(width);

            // Pitch or Linear Size (for DXT1: max(1, ((width+3)/4)) * blockSize)
            int linearSize;
            if (type == DDSType.DXT1)
            {
                linearSize = Math.Max(1, ((width + 3) / 4)) * 8 * ((height + 3) / 4);
            }
            else
            {
                linearSize = Math.Max(1, ((width + 3) / 4)) * 16 * ((height + 3) / 4);
            }
            writer.Write(linearSize);

            // Depth
            writer.Write(0);

            // MipMap count
            writer.Write(Math.ILogB(Math.Max(width, height)) + 1);

            // Reserved (11 ints)
            for (int i = 0; i < 11; i++)
            {
                writer.Write(0);
            }

            // Pixel format (DDS_PIXELFORMAT)
            writer.Write(32);                // size
            writer.Write(0x00000004);        // flags (FOURCC)
            switch (type)
            {
                case DDSType.DXT1:
                    writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'1' }); // FOURCC
                    break;
                case DDSType.DXT5:
                    writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'5' }); // FOURCC
                    break;
                case DDSType.ATI2:
                    writer.Write(new[] { (byte)'A', (byte)'T', (byte)'I', (byte)'2' }); // FOURCC
                    break;
            }
            writer.Write(0); // RGBBitCount
            writer.Write(0); // RBitMask
            writer.Write(0); // GBitMask
            writer.Write(0); // BBitMask
            writer.Write(0); // ABitMask

            // Caps
            writer.Write(0x401008);

            // Caps2, Caps3, Caps4
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            // Reserved2
            writer.Write(0);

            return header.ToArray();
        }
        public static byte[] ConvertToDDS(byte[] tex, bool highRes = false, byte[]? pit = null)
        {
            int width;
            int height;
            if (highRes)
            {
                width = BitConverter.ToInt16(tex, 4);
                height = BitConverter.ToInt16(tex, 6);
                if(width == 0 || height == 0)
                {
                    // Texture does not have an HD equivalent
                    width = BitConverter.ToInt16(tex, 0);
                    height = BitConverter.ToInt16(tex, 2);
                    highRes = false;
                }
            }
            else
            {
                width = BitConverter.ToInt16(tex, 0);
                height = BitConverter.ToInt16(tex, 2);
            }

            int pixelFormat = tex[16];

            DDSType ddsFormat;
            bool isX360 = false;

            int length;
            if (highRes)
            {
                length = BitConverter.ToInt32(tex, 24);
            }
            else
            {
                length = BitConverter.ToInt32(tex, 20);
            }
            int hdFileOffset = BitConverter.ToInt32(tex, 28);

            switch (pixelFormat)
            {
                case 1:
                    ddsFormat = DDSType.DXT1;
                    break;
                case 5: // Not 100% sure if this is the case yet
                    ddsFormat = DDSType.DXT5;
                    break;
                case 82:
                    ddsFormat = DDSType.DXT1;
                    isX360 = true;
                    break;
                case 84:
                    ddsFormat = DDSType.DXT5;
                    isX360 = true;
                    break;
                case 113:
                    ddsFormat = DDSType.ATI2;
                    isX360 = true;
                    break;
                default:
                    Debug.Fail("Unknown pixel format " + pixelFormat);
                    ddsFormat = DDSType.DXT1;
                    break;
            }

            byte[] header = CreateDDSHeader(width, height, ddsFormat);
            byte[] data = new byte[length];
            if (highRes)
            {
                Buffer.BlockCopy(pit!, hdFileOffset, data, 0, length);
            }
            else
            {
                Buffer.BlockCopy(tex, 32, data, 0, length);
            }

            if (isX360)
            {
                // Untile the main texture
                byte[] untiledData = Xbox360Utils.Untile360DXT(data, 0, width, height, ddsFormat == DDSType.DXT1 ? 8 : 16);
                Buffer.BlockCopy(untiledData, 0, data, 0, untiledData.Length);

                // Untile all the mipmaps
                int numMipmaps = Math.ILogB(Math.Max(width, height));
                int dataOffset = untiledData.Length;
                for (int i = 0; i < numMipmaps; i++)
                {
                    width = Math.Max(1, width / 2);
                    height = Math.Max(1, height / 2);
                    byte[] mipmapData = Xbox360Utils.Untile360DXT(data, dataOffset, width, height, ddsFormat == DDSType.DXT1 ? 8 : 16);
                    Buffer.BlockCopy(mipmapData, 0, data, dataOffset, mipmapData.Length);
                    dataOffset += mipmapData.Length;
                }
            }

            byte[] result = new byte[header.Length + data.Length];

            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(data, 0, result, header.Length, data.Length);

            return result;
        }
    }
}
