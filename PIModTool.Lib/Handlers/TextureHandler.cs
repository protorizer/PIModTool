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
        private static byte[] CreateDDSHeader(int width, int height, int numLevels, DDSType type)
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
            writer.Write(numLevels);

            // Reserved (11 ints)
            for (int i = 0; i < 11; i++)
            {
                writer.Write(0);
            }

            // Pixel format (DDS_PIXELFORMAT)
            writer.Write(32);                // size
            if(type != DDSType.X360_A8R8G8B8)
            {
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
                    default:
                        Debug.Fail($"Unsupported pixel format {type}");
                        writer.Write(new[] { (byte)'D', (byte)'X', (byte)'T', (byte)'1' }); // FOURCC
                        break;
                }
                writer.Write(0); // RGBBitCount
                writer.Write(0); // RBitMask
                writer.Write(0); // GBitMask
                writer.Write(0); // BBitMask
                writer.Write(0); // ABitMask
            }
            else // Uncompressed texture
            {
                writer.Write(0x00000041); // flags (RGB)
                writer.Write(0); // FOURCC (UNUSED)
                writer.Write(32); // RGBBitCount
                writer.Write(0x00FF0000); // RBitMask
                writer.Write(0x0000FF00); // GBitMask
                writer.Write(0x000000FF); // BBitMask
                writer.Write(0xFF000000); // ABitMask
            }

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

            // FA1 has a slightly different texture format than every other game, likely due to the engine not having volume texture support at the time
            // The two volume texture bytes are not present, and two seemingly blank bytes are inserted after streamPriority
            // Since StreamPriority is never 0, but there are padding bytes in the position where it normally would be in FA1 files, I use this to detect the FA1 format
            // Probably a better way to do this but if it works it works
            if(tex.StreamPriority == 0)
            {
                tex.Game = Game.FA1;
                tex.StreamPriority = tex.HDMipmapCount;
                tex.Type = (TextureType)tex.MipmapCount;
                tex.HDMipmapCount = tex.HDTextureDepth;
                tex.MipmapCount = tex.VolumeTextureDepth;
                tex.HDTextureDepth = 0;
                tex.VolumeTextureDepth = 0;
            }

            return tex;
        }

        private static byte[] WritePITexture(PITexture tex)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(tex.Width);
            writer.Write(tex.Height);
            writer.Write(tex.HDWidth);
            writer.Write(tex.HDHeight);
            writer.Write(tex.Padding);
            if (!(tex.Game == Game.FA1))
            {
                writer.Write(tex.VolumeTextureDepth);
                writer.Write(tex.HDTextureDepth);
            }
            writer.Write(tex.MipmapCount);
            writer.Write(tex.HDMipmapCount);
            writer.Write((byte)tex.Type);
            writer.Write(tex.StreamPriority);
            if (tex.Game == Game.FA1)
            {
                writer.Write(tex.Padding);
            }
            writer.Write((uint)tex.TextureFormat);
            writer.Write(tex.DataSize);
            writer.Write(tex.HDDataSize);
            writer.Write(tex.HDStreamOffset);

            writer.Write(tex.Data);

            return stream.ToArray();
        }

        private static bool IsX360Tex(DDSType type)
        {
            return (int)type > 10; // X360 formats are all big ints, PS3 are all small ints. 10 is an arbitrary number
        }

        private static int GetTexelBytePitch(DDSType type)
        {
            if(type == DDSType.X360_A8R8G8B8)
            {
                // Uncompressed
                return 4;
            }
            else if(type == DDSType.X360_DXT1)
            {
                return 8;
            }
            return 16;
        }
        private static int GetBlockPixelSize(DDSType type)
        {
            if(type == DDSType.X360_A8R8G8B8)
            {
                // Uncompressed
                return 1;
            }
            return 4;
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
                    // TODO: display a no HD texture messaage instead
                }
            }
            else
            {
                width = tex.Width;
                height = tex.Height;
            }

            int numLevels = highRes ? tex.HDMipmapCount : tex.MipmapCount;
            byte[] header = CreateDDSHeader(width, height, numLevels, tex.TextureFormat);
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

                int texelBytePitch = GetTexelBytePitch(tex.TextureFormat);
                int blockPixelSize = GetBlockPixelSize(tex.TextureFormat);
                byte[][] imageData = new byte[numLevels][];

                // Swap endianness
                if (blockPixelSize == 1)
                {
                    Xbox360Utils.SwapEndianWord(data);
                }
                else
                {
                    Xbox360Utils.SwapEndianShort(data);
                }

                // Low resolution mipmap levels are packed into a single shared region, we need to track and handle these differently
                int packedMipBase = Xbox360Utils.GetPackedMipBase(width, height);
                List<int> packedLevels = new List<int>();
                List<int> packedWidth = new List<int>();
                List<int> packedHeight = new List<int>();

                // Untile normal mipmaps
                int dataOffset = 0;
                int mipWidth = width;
                int mipHeight = height;
                for (int i = 0; i < numLevels; i++)
                {
                    // Normal mipmap
                    if(i < packedMipBase)
                    {
                        imageData[i] = Xbox360Utils.ConvertX360Image(data, dataOffset, mipWidth, mipHeight, texelBytePitch, blockPixelSize);
                        dataOffset += Xbox360Utils.GetTiledLevelSize(mipWidth, mipHeight, texelBytePitch); // Normal mipmaps are always written to at minimum one chunk
                    }
                    // Packed mipmap, record the data to convert the entire chunk at the end
                    else
                    {
                        packedLevels.Add(i);
                        packedWidth.Add(mipWidth);
                        packedHeight.Add(mipHeight);
                    }
                    mipWidth = Math.Max(1, mipWidth / 2);
                    mipHeight = Math.Max(1, mipHeight / 2);
                }

                // Unpack and untile the packed region
                if(packedLevels.Count > 0)
                {
                    byte[][] packedData = Xbox360Utils.ConvertX360PackedMipTail(data, dataOffset, width, height, packedLevels.ToArray(), packedWidth.ToArray(), packedHeight.ToArray(), texelBytePitch, blockPixelSize);
                    for (int i = 0; i < packedLevels.Count; i++)
                    {
                        imageData[packedLevels[i]] = packedData[i];
                    }
                }

                // We need to rewrite data with a different size because the packed mips throw off the initial calculations
                using var outputStream = new MemoryStream();
                for (int i = 0; i < imageData.Length; i++)
                {
                    outputStream.Write(imageData[i], 0, imageData[i].Length);
                }

                data = outputStream.ToArray();
                Xbox360Utils.SwapEndianWord(data);
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

            if (texWidth == 0 || texHeight == 0)
            {
                // Texture does not have an HD equivalent
                texWidth = tex.Width;
                texHeight = tex.Height;
                highRes = false;
            }

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

            byte[] outputData;

            if (IsX360Tex(tex.TextureFormat))
            {
                int texelBytePitch = GetTexelBytePitch(ddsFormat);
                int blockPixelSize = GetBlockPixelSize(ddsFormat);

                // Remove header and swap to big endian
                byte[] linearData = new byte[dds.Length - 128];
                Buffer.BlockCopy(dds, 128, linearData, 0, linearData.Length);
                if (blockPixelSize == 1)
                {
                    Xbox360Utils.SwapEndianWord(linearData);
                }
                else
                {
                    Xbox360Utils.SwapEndianShort(linearData);
                }

                // Swizzle texture
                int packedMipBase = Xbox360Utils.GetPackedMipBase(ddsWidth, ddsHeight);

                var packedLevels = new List<int>();
                var packedWidths = new List<int>();
                var packedHeights = new List<int>();
                var packedLinearData = new List<byte[]>();

                using var outputStream = new MemoryStream();
                int srcOffset = 0;
                int mipWidth = ddsWidth;
                int mipHeight = ddsHeight;

                int packedTailSrcOffset = -1;
                for (int i = 0; i < ddsMipMaps; i++)
                {
                    int wBlocks = Math.Max(1, mipWidth / 4);
                    int hBlocks = Math.Max(1, mipHeight / 4);
                    int levelSize = wBlocks * hBlocks * texelBytePitch;

                    if (i < packedMipBase)
                    {
                        // Normal mipmap - individually tiled, always occupies a whole
                        // number of 32x32-block tiles (see GetTiledLevelSize)
                        byte[] tiledLevel = Xbox360Utils.ConvertX360Image(linearData, srcOffset, mipWidth, mipHeight, texelBytePitch, blockPixelSize, true);
                        outputStream.Write(tiledLevel, 0, tiledLevel.Length);
                    }
                    else
                    {
                        if (packedTailSrcOffset < 0)
                        {
                            packedTailSrcOffset = srcOffset; // Track the offset at which packed mips start
                        }
                        packedLevels.Add(i);
                        packedWidths.Add(mipWidth);
                        packedHeights.Add(mipHeight);
                    }

                    srcOffset += levelSize;
                    mipWidth = Math.Max(1, mipWidth / 2);
                    mipHeight = Math.Max(1, mipHeight / 2);
                }

                // Swizzle packed levels
                if (packedLevels.Count > 0)
                {
                    byte[][] packedResult = Xbox360Utils.ConvertX360PackedMipTail(linearData, packedTailSrcOffset, ddsWidth, ddsHeight, packedLevels.ToArray(), packedWidths.ToArray(), packedHeights.ToArray(), texelBytePitch, blockPixelSize, true);
                    outputStream.Write(packedResult[0], 0, packedResult[0].Length);
                }

                outputData = outputStream.ToArray();
                Xbox360Utils.SwapEndianWord(outputData);
            }
            else
            {
                // PS3 - no need for swizzling so nice and simple. Man why can't all the formats be like this
                outputData = new byte[dds.Length - 128];
                Buffer.BlockCopy(dds, 128, outputData, 0, outputData.Length);
            }

            // Write data
            if (highRes)
            {
                Buffer.BlockCopy(outputData, 0, pit, (int)tex.HDStreamOffset, outputData.Length);
            }
            else
            {
                Buffer.BlockCopy(outputData, 0, tex.Data, 0, outputData.Length);
            }

            byte[] newTexData = WritePITexture(tex);
            Buffer.BlockCopy(newTexData, 0, texData, 0, newTexData.Length);

            return 0;
        }
    }
}
