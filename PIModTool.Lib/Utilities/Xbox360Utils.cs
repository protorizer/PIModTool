using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib.Utilities
{
    // Utilities related to X360-spedific file manipulation

    public static class Xbox360Utils
    {
        private static int XGAddress2DTiledX(int offset, int widthInBlocks, int texelPitch)
        {
            int alignedWidth = (widthInBlocks + 31) & ~31;
            int logBpp = Math.ILogB(texelPitch);

            int offsetB = offset << logBpp;
            int offsetT = ((offsetB & ~4095) >> 3) + ((offsetB & 1792) >> 2) + (offsetB & 63);
            int offsetM = offsetT >> (7 + logBpp);

            int macroX = ((offsetM % (alignedWidth >> 5)) << 2);
            int tile = ((((offsetT >> (5 + logBpp)) & 2) + (offsetB >> 6)) & 3);
            int macro = (macroX + tile) << 3;

            int micro = ((((offsetT >> 1) & ~15) + (offsetT & 15)) & ((texelPitch << 3) - 1)) >> logBpp;

            return macro + micro;
        }

        private static int XGAddress2DTiledY(int offset, int widthInBlocks, int texelPitch)
        {
            int alignedWidth = (widthInBlocks + 31) & ~31;
            int logBpp = Math.ILogB(texelPitch);

            int offsetB = offset << logBpp;
            int offsetT = ((offsetB & ~4095) >> 3) + ((offsetB & 1792) >> 2) + (offsetB & 63);
            int offsetM = offsetT >> (7 + logBpp);

            int macroY = ((offsetM / (alignedWidth >> 5)) << 2);
            int tile = ((offsetT >> (6 + logBpp)) & 1) + (((offsetB & 2048) >> 10));
            int macro = (macroY + tile) << 3;

            int micro = ((((offsetT & (((texelPitch << 6) - 1) & ~31)) + ((offsetT & 15) << 1))
                           >> (3 + logBpp)) & ~1);

            return macro + micro + ((offsetT & 16) >> 4);
        }

        // Untiles a tiled & swizzled Xbox 360 texture
        public static byte[] Untile360DXT(byte[] source, int offset, int width, int height, int blockBytes, bool swapEndian16 = true)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("Width and height must be positive.");
            if (blockBytes != 8 && blockBytes != 16)
                throw new ArgumentException("blockBytes must be 8 (DXT1) or 16 (DXT3/DXT5/ATI2).");

            int blockWidth = Math.Max(1, width / 4);
            int blockHeight = Math.Max(1, height / 4);
            int alignedBlockWidth = (blockWidth + 31) & ~31;
            int alignedBlockHeight = (blockHeight + 31) & ~31;

            int linearSize = blockWidth * blockHeight * blockBytes;

            if (source.Length - offset < linearSize)
                throw new ArgumentException("Source buffer is smaller than the requested texture size.");

            byte[] input = new byte[source.Length - offset];
            Buffer.BlockCopy(source, offset, input, 0, input.Length);
            if (swapEndian16)
            {
                for (int i = 0; i + 1 < input.Length; i += 2)
                    (input[i], input[i + 1]) = (input[i + 1], input[i]);
            }

            byte[] output = new byte[linearSize];

            int blockNum = 0;
            // Iterate over the aligned block space - the tiling scheme addresses
            // into the full aligned region even for textures smaller than 128px
            for (int blockY = 0; blockY < alignedBlockHeight; blockY++)
            {
                for (int blockX = 0; blockX < alignedBlockWidth; blockX++)
                {
                    int blockOffset = blockY * alignedBlockWidth + blockX;

                    int tiledX = XGAddress2DTiledX(blockOffset, blockWidth, blockBytes);
                    int tiledY = XGAddress2DTiledY(blockOffset, blockWidth, blockBytes);

                    // Skip blocks that fall outside the real texture dimensions
                    if (tiledX >= blockWidth || tiledY >= blockHeight)
                        continue;

                    // Source is the flat tiled buffer read sequentially (no aligned padding on disk)
                    int srcOffset = blockNum * blockBytes;
                    int dstOffset = (tiledY * blockWidth + tiledX) * blockBytes;

                    Buffer.BlockCopy(input, srcOffset, output, dstOffset, blockBytes);
                    blockNum++;
                }
            }

            return output;
        }
    }
}
