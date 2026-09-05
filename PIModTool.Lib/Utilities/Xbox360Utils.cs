using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PIModTool.Lib.Utilities
{
    // Utilities related to X360-spedific file manipulation
    // Swizzling logic adopted from https://github.com/bartlomiejduda/ReverseBox/blob/main/reversebox/image/swizzling/swizzle_x360.py
    // Packed mipmap logic adopted from https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/gpu/texture_util.cc

    public static class Xbox360Utils
    {
        private static int XGAddress2DTiledX(int blockOffset, int widthInBlocks, int texelBytePitch)
        {
            int alignedWidth = (widthInBlocks + 31) & ~31;
            int logBpp = Math.ILogB(texelBytePitch);
            int offsetByte = blockOffset << logBpp;
            int offsetTile = (((offsetByte & ~0xFFF) >> 3) + ((offsetByte & 0x700) >> 2) + (offsetByte & 0x3F));
            int offsetMacro = offsetTile >> (7 + logBpp);

            int macroX = (offsetMacro % (alignedWidth >> 5)) << 2;
            int tile = (((offsetTile >> (5 + logBpp)) & 2) + (offsetByte >> 6)) & 3;
            int macro = (macroX + tile) << 3;
            int micro = ((((offsetTile >> 1) & ~0xF) + (offsetTile & 0xF)) & ((texelBytePitch << 3) - 1)) >> logBpp;

            return macro + micro;
        }

        private static int XGAddress2DTiledY(int blockOffset, int widthInBlocks, int texelBytePitch)
        {
            int alignedWidth = (widthInBlocks + 31) & ~31;
            int logBpp = Math.ILogB(texelBytePitch);
            int offsetByte = blockOffset << logBpp;
            int offsetTile = (((offsetByte & ~0xFFF) >> 3) + ((offsetByte & 0x700) >> 2) + (offsetByte & 0x3F));
            int offsetMacro = offsetTile >> (7 + logBpp);

            int macroY = (offsetMacro / (alignedWidth >> 5)) << 2;
            int tile = ((offsetTile >> (6 + logBpp)) & 1) + ((offsetByte & 0x800) >> 10);
            int macro = (macroY + tile) << 3;
            int micro = (((offsetTile & ((texelBytePitch << 6) - 1 & ~0x1F)) + ((offsetTile & 0xF) << 1)) >> (3 + logBpp)) & ~1;

            return macro + micro + ((offsetTile & 0x10) >> 4);
        }

        // Int32.Log2 always floors decimal values
        // However we need to ceil some logs here
        private static int Log2Ceil(int x)
        {
            if (x <= 1) { return 0; }
            int v = x - 1;
            int r = 0;
            while (v > 0) { 
                v >>= 1; 
                r++; 
            }
            return r;
        }

        // Returns the mip level corresponding to 32x32 resolution, which is when packed mipmaps start
        public static int GetPackedMipBase(int baseWidth, int baseHeight)
        {
            int log2Width = Log2Ceil(baseWidth);
            int log2Height = Log2Ceil(baseHeight);
            int log2Size = Math.Min(log2Width, log2Height);
            return log2Size > 4 ? log2Size - 4 : 0;
        }

        // Ported from Xenia's GetPackedMipOffset (texture_util.cc). Returns the block-space
        // offset, within the shared 32x32-block tile, where a given packed mip's data begins
        private static (int xBlocks, int yBlocks) GetPackedMipOffset(int baseWidth, int baseHeight, int mip, int blockWidthShift, int blockHeightShift)
        {
            int log2Width = Log2Ceil(baseWidth);
            int log2Height = Log2Ceil(baseHeight);
            int packedMipBase = GetPackedMipBase(baseWidth, baseHeight);
            int packedMip = mip - packedMipBase;

            int xBlocks, yBlocks;
            if (packedMip < 3)
            {
                if (log2Width > log2Height)
                {
                    xBlocks = 0;
                    yBlocks = 16 >> packedMip;
                }
                else
                {
                    xBlocks = 16 >> packedMip;
                    yBlocks = 0;
                }
            }
            else
            {
                if (log2Width > log2Height)
                {
                    xBlocks = (1 << (log2Width - packedMipBase)) >> (packedMip - 2);
                    yBlocks = 0;
                }
                else
                {
                    xBlocks = 0;
                    yBlocks = (1 << (log2Height - packedMipBase)) >> (packedMip - 2);
                }
            }

            return (xBlocks >> blockWidthShift, yBlocks >> blockHeightShift);
        }

        // Size in bytes that each mipmap tile takes up - effectively in increments of 1024*texelBytePitch
        public static int GetTiledLevelSize(int imageWidth, int imageHeight, int texelBytePitch)
        {
            int blockPixelSize = 4;
            int widthInBlocks = Math.Max(1, imageWidth / blockPixelSize);
            int heightInBlocks = Math.Max(1, imageHeight / blockPixelSize);
            int paddedWidthInBlocks = (widthInBlocks + 31) & ~31;
            int paddedHeightInBlocks = (heightInBlocks + 31) & ~31;
            return paddedWidthInBlocks * paddedHeightInBlocks * texelBytePitch;
        }

        /*
         * Convert image data to/from X360 format
         * blockPixelSize: 1 for uncompressed, 4 for compressed
         * texelBytePitch: 4 for uncompressed, 8 for DXT1, otherwise 16
         * convertBack: re-swizzle back to X360 instead of decoding
         */
        public static byte[] ConvertX360Image(byte[] tex, int offset, int imageWidth, int imageHeight, int texelBytePitch, int blockPixelSize, bool convertBack=false)
        {
            byte[] imageData = new byte[tex.Length - offset];
            Buffer.BlockCopy(tex, offset, imageData, 0, imageData.Length);

            int widthInBlocks = Math.Max(1, imageWidth / blockPixelSize);
            int heightInBlocks = Math.Max(1, imageHeight / blockPixelSize);

            int paddedWidthInBlocks = (widthInBlocks + 31) & ~31;
            int paddedHeightInBlocks = (heightInBlocks + 31) & ~31;
            int totalPaddedBlocks = paddedWidthInBlocks * paddedHeightInBlocks;

            byte[] convertedData = convertBack
                ? new byte[totalPaddedBlocks * texelBytePitch]
                : new byte[widthInBlocks * heightInBlocks * texelBytePitch];

            for (int blockOffset = 0; blockOffset < totalPaddedBlocks; blockOffset++)
            {
                int x = XGAddress2DTiledX(blockOffset, paddedWidthInBlocks, texelBytePitch);
                int y = XGAddress2DTiledY(blockOffset, paddedWidthInBlocks, texelBytePitch);

                if (x >= widthInBlocks || y >= heightInBlocks)
                    continue;

                int srcByteOffset;
                int destByteOffset;

                if (!convertBack)
                {
                    srcByteOffset = blockOffset * texelBytePitch;
                    destByteOffset = (y * widthInBlocks + x) * texelBytePitch;
                }
                else
                {
                    srcByteOffset = (y * widthInBlocks + x) * texelBytePitch;
                    destByteOffset = blockOffset * texelBytePitch;
                }

                if (srcByteOffset + texelBytePitch > imageData.Length)
                    continue;

                Buffer.BlockCopy(imageData, srcByteOffset, convertedData, destByteOffset, texelBytePitch);
            }

            return convertedData;
        }

        /*
         * Decodes all packed-tail mip levels (mip index >= GetPackedMipBase) in a
         * single pass, since they're interleaved within one shared tiled region and
         * can't be decoded independently. mipIndices/mipWidths/mipHeights must be
         * parallel arrays covering every packed level, smallest-base-relative index
         * first. Returns one untiled byte[] per level, in the same order.
         *
         * offset must point at the start of the shared packed-tail region, i.e. right
         * after (numLevelsBelowPackedBase * 1024 * texelBytePitch) bytes of
         * individually-tiled level data.
         * 
         * convertBack: re-swizzle back to X360 instead of decoding
         */
        public static byte[][] ConvertX360PackedMipTail(byte[] tex, int offset, int baseWidth, int baseHeight, int[] mipIndices, int[] mipWidths, int[] mipHeights, int texelBytePitch, int blockPixelSize, bool convertBack=false)
        {
            byte[] imageData = new byte[tex.Length - offset];
            Buffer.BlockCopy(tex, offset, imageData, 0, imageData.Length);

            int blockWidthShift = Math.ILogB(blockPixelSize);
            int blockHeightShift = Math.ILogB(blockPixelSize);

            int n = mipIndices.Length;
            var rectX = new int[n];
            var rectY = new int[n];
            var rectW = new int[n];
            var rectH = new int[n];
            var levelLinearOffset = new int[n]; // this level's start within the combined linear buffer
            int totalLinearSize = 0;

            for (int i = 0; i < n; i++)
            {
                int wBlocks = Math.Max(1, mipWidths[i] / blockPixelSize);
                int hBlocks = Math.Max(1, mipHeights[i] / blockPixelSize);
                var (ox, oy) = GetPackedMipOffset(baseWidth, baseHeight, mipIndices[i], blockWidthShift, blockHeightShift);
                rectX[i] = ox; rectY[i] = oy; rectW[i] = wBlocks; rectH[i] = hBlocks;
                levelLinearOffset[i] = totalLinearSize;
                totalLinearSize += wBlocks * hBlocks * texelBytePitch;
            }

            byte[] linearBuffer;
            byte[] tiledBuffer;
            if (!convertBack)
            {
                tiledBuffer = new byte[tex.Length - offset];
                Buffer.BlockCopy(tex, offset, tiledBuffer, 0, tiledBuffer.Length);
                linearBuffer = new byte[totalLinearSize];
            }
            else
            {
                linearBuffer = new byte[tex.Length - offset];
                Buffer.BlockCopy(tex, offset, linearBuffer, 0, linearBuffer.Length);
                tiledBuffer = new byte[GetPackedMipTailSize(texelBytePitch)];
            }

            const int paddedWidthInBlocks = 32;
            const int totalPaddedBlocks = 32 * 32;

            for (int blockOffset = 0; blockOffset < totalPaddedBlocks; blockOffset++)
            {
                int x = XGAddress2DTiledX(blockOffset, paddedWidthInBlocks, texelBytePitch);
                int y = XGAddress2DTiledY(blockOffset, paddedWidthInBlocks, texelBytePitch);

                for (int i = 0; i < n; i++)
                {
                    if (x >= rectX[i] && x < rectX[i] + rectW[i] && y >= rectY[i] && y < rectY[i] + rectH[i])
                    {
                        int localX = x - rectX[i];
                        int localY = y - rectY[i];
                        int tiledByteOffset = blockOffset * texelBytePitch;
                        int linearByteOffset = levelLinearOffset[i] + (localY * rectW[i] + localX) * texelBytePitch;

                        int srcByteOffset = convertBack ? linearByteOffset : tiledByteOffset;
                        int destByteOffset = convertBack ? tiledByteOffset : linearByteOffset;
                        byte[] src = convertBack ? linearBuffer : tiledBuffer;
                        byte[] dest = convertBack ? tiledBuffer : linearBuffer;

                        if (srcByteOffset + texelBytePitch <= src.Length && destByteOffset + texelBytePitch <= dest.Length)
                        {
                            Buffer.BlockCopy(src, srcByteOffset, dest, destByteOffset, texelBytePitch);
                        }
                        break;
                    }
                }
            }

            if (convertBack)
            {
                return new byte[][] { tiledBuffer };
            }

            // Split the combined linear buffer back into one array per level.
            byte[][] outputs = new byte[n][];
            for (int i = 0; i < n; i++)
            {
                int size = rectW[i] * rectH[i] * texelBytePitch;
                outputs[i] = new byte[size];
                Buffer.BlockCopy(linearBuffer, levelLinearOffset[i], outputs[i], 0, size);
            }
            return outputs;
        }

        // What I've observed from every file I've inspected manually
        // Not verified in code, so may be different on some files, so keeping as a function incase I find something different
        public static int GetPackedMipTailSize(int texelBytePitch)
        {
            return 4096;
        }

        // Swap endianness of short data
        public static void SwapEndianShort(byte[] data)
        {
            Span<ushort> values = MemoryMarshal.Cast<byte, ushort>(data);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
            }
        }

        // Swap endianness of 4-byte data
        public static void SwapEndianWord(byte[] data)
        {
            Span<uint> values = MemoryMarshal.Cast<byte, uint>(data);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
            }
        }
    }
}
