using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PIModTool.Lib.Utilities
{
    // Utilities related to X360-spedific file manipulation
    // Swizzling logic adopted from https://github.com/bartlomiejduda/ReverseBox/blob/main/reversebox/image/swizzling/swizzle_x360.py

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

        /*
         * Convert image data to/from X360 format
         * texelBytePitch: 8 for DXT1, otherwise 16
         * convertBack: re-swizzle back to X360 instead of decoding
         */
        public static byte[] ConvertX360Image(byte[] tex, int offset, int imageWidth, int imageHeight, int texelBytePitch, bool convertBack=false)
        {
            byte[] imageData = new byte[tex.Length - offset];
            Buffer.BlockCopy(tex, offset, imageData, 0, imageData.Length);

            // If the texture is the size of 1 block or less, it can't be swizzled
            if (imageWidth < 64 || imageHeight < 64)
            {
                return imageData;
            }

            int blockPixelSize = 4; // Comprepssed DDS has 4 pixels per block
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

        // Swap endianness of short data
        public static void SwapEndianShort(byte[] data)
        {
            Span<ushort> values = MemoryMarshal.Cast<byte, ushort>(data);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
            }
        }
    }
}
