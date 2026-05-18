using PIModTool.Lib.Types;
using PIModTool.Lib.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PIModTool.Lib
{
    public static class TextureHandler
    {
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
            if (type == DDSType.PS3_DXT1 || type == DDSType.X360_DXT1)
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
                case DDSType.PS3_DXT1:
                case DDSType.X360_DXT1:
                    writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'1' }); // FOURCC
                    break;
                case DDSType.PS3_DXT5:
                case DDSType.X360_DXT5:
                    writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'5' }); // FOURCC
                    break;
                case DDSType.X360_DXN:
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

        private static PITexture ReadPITexture(byte[] data)
        {
            using MemoryStream stream = new MemoryStream(data);
            using BinaryReader reader = new BinaryReader(stream);

            PITexture tex = new PITexture
            {
                Width = reader.ReadUInt16(),
                Height = reader.ReadUInt16(),
                HDWidth = reader.ReadUInt16(),
                HDHeight = reader.ReadUInt16(),
                Padding = reader.ReadUInt16(),
                VolumeTextureDepth = reader.ReadByte(),
                HDTextureDepth = reader.ReadByte(),
                MipmapCount = reader.ReadByte(),
                HDMipmapCount = reader.ReadByte(),
                Type = EnumUtils.TryConvertByte<TextureType>(reader.ReadByte()),
                StreamPriority = reader.ReadByte(),
                TextureFormat = EnumUtils.TryConvertUInt<DDSType>(reader.ReadUInt32()),
                DataSize = reader.ReadUInt32(),
                HDDataSize = reader.ReadUInt32(),
                HDStreamOffset = reader.ReadUInt32(),
            };
            tex.Data = reader.ReadBytes((int)tex.DataSize);

            return tex;
        }

        private static bool IsX360Tex(DDSType type)
        {
            return (int)type > 10; // X360 formats are all big ints, PS3 are all small ints. 10 is an arbitrary number
        }

        public static byte[] ConvertToDDS(byte[] texData, bool highRes = false, byte[]? pit = null)
        {
            PITexture tex = ReadPITexture(texData);
            int width;
            int height;
            if (highRes)
            {
                width = tex.HDWidth;
                height = tex.HDHeight;
                if(width == 0 || height == 0)
                {
                    // Texture does not have an HD equivalent
                    width = tex.Width;
                    height = tex.Height;
                    highRes = false;
                }
            }
            else
            {
                width = tex.Width;
                height = tex.Height;
            }

            byte[] header = CreateDDSHeader(width, height, tex.TextureFormat);
            byte[] data = new byte[highRes ? tex.HDDataSize : tex.DataSize];
            if (highRes)
            {
                Buffer.BlockCopy(pit!, (int)tex.HDStreamOffset, data, 0, data.Length);
            }
            else
            {
                Buffer.BlockCopy(tex.Data, 0, data, 0, data.Length);
            }

            if (IsX360Tex(tex.TextureFormat))
            {
                // Swap endianness
                Xbox360Utils.SwapEndianShort(data);

                // Untile the main texture
                byte[] untiledData = Xbox360Utils.ConvertX360Image(data, 0, width, height, tex.TextureFormat == DDSType.X360_DXT1 ? 8 : 16);
                Buffer.BlockCopy(untiledData, 0, data, 0, untiledData.Length);

                // Untile all the mipmaps
                int numMipmaps = Math.ILogB(Math.Max(width, height));
                int dataOffset = untiledData.Length;
                for (int i = 0; i < numMipmaps; i++)
                {
                    width = Math.Max(1, width / 2);
                    height = Math.Max(1, height / 2);
                    byte[] mipmapData = Xbox360Utils.ConvertX360Image(data, dataOffset, width, height, tex.TextureFormat == DDSType.X360_DXT1 ? 8 : 16);
                    Buffer.BlockCopy(mipmapData, 0, data, dataOffset, mipmapData.Length);
                    dataOffset += mipmapData.Length;
                }
            }

            byte[] result = new byte[header.Length + data.Length];

            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(data, 0, result, header.Length, data.Length);

            return result;
        }

        // Replace the data of a PI texture with that of the given DDS file
        // The contents of tex or pit are set to the new file data
        // Returns an error code or 0 if success
        // -1: Wrong resolution
        // -2: Wrong compression format
        // -3: Wrong mipmap count
        public static int ReplacePITex(byte[] texData, byte[] dds, bool highRes = false, byte[]? pit = null)
        {
            PITexture tex = ReadPITexture(texData);

            // Verify resolution
            int texWidth = highRes ? tex.HDWidth : tex.Width;
            int texHeight = highRes ? tex.HDHeight : tex.Height;

            int ddsHeight = BitConverter.ToInt32(dds, 12);
            int ddsWidth = BitConverter.ToInt32(dds, 16);

            if(texWidth != ddsWidth || texHeight != ddsHeight)
            {
                return -1;
            }

            // Verify format
            string ddsFormatStr = Encoding.UTF8.GetString([dds[84], dds[85], dds[86], dds[87]]);
            DDSType ddsFormat;
            // Defaulting to X360 format since we know these values for sure due to the disassembly
            switch (ddsFormatStr)
            {
                case "DXT1":
                    ddsFormat = DDSType.X360_DXT1;
                    break;
                case "DXT5":
                    ddsFormat = DDSType.X360_DXT5;
                    break;
                case "ATI2":
                    ddsFormat = DDSType.X360_DXN;
                    break;
                default:
                    return -2;
            }
            if(!ddsFormat.IsEqual(tex.TextureFormat)) // IsEqual accounts for different platforms
            {
                return -2;
            }

            // Verify mipmaps
            int texMipMaps = highRes ? tex.HDMipmapCount : tex.MipmapCount;
            int ddsMipMaps = BitConverter.ToInt32(dds, 28);
            if(texMipMaps != ddsMipMaps)
            {
                return -3;
            }

            // X360 prep
            if (IsX360Tex(tex.TextureFormat))
            {
                // Swap to big endian
                Xbox360Utils.SwapEndianShort(dds);
                // Swizzle texture
                byte[] swizzledDDS = Xbox360Utils.ConvertX360Image(dds, 128, ddsWidth, ddsHeight, ddsFormat == DDSType.X360_DXT1 ? 8 : 16, true);
                Buffer.BlockCopy(swizzledDDS, 0, dds, 128, swizzledDDS.Length);
                // Swizzle mipmaps (TODO)
                int dataOffset = swizzledDDS.Length + 128;
                for (int i = 0; i < ddsMipMaps - 1; i++)
                {
                    ddsWidth = Math.Max(1, ddsWidth / 2);
                    ddsHeight = Math.Max(1, ddsHeight / 2);
                    byte[] mipmapData = Xbox360Utils.ConvertX360Image(dds, dataOffset, ddsWidth, ddsHeight, ddsFormat == DDSType.X360_DXT1 ? 8 : 16);
                    Buffer.BlockCopy(mipmapData, 0, dds, dataOffset, mipmapData.Length);
                    dataOffset += mipmapData.Length;
                }
            }


            // Write data
            int ddsDataLen = dds.Length - 128;
            if (highRes)
            {
                Buffer.BlockCopy(dds, 128, pit, (int)tex.HDStreamOffset, ddsDataLen);
            }
            else
            {
                Buffer.BlockCopy(dds, 128, tex.Data, 0, ddsDataLen);
            }

            return 0;
        }
    }
}
