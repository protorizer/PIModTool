namespace PIModTool.Lib.Utilities
{
    public static class PS3Utils
    {
        private const int MinLevelBytes = 16;

        // Spreads the low 16 bits of v out to the even bit positions
        private static uint SpreadBits(uint v)
        {
            v &= 0x0000FFFF;
            v = (v | (v << 8)) & 0x00FF00FF;
            v = (v | (v << 4)) & 0x0F0F0F0F;
            v = (v | (v << 2)) & 0x33333333;
            v = (v | (v << 1)) & 0x55555555;
            return v;
        }

        // Inverse of SpreadBits: gathers the even bits of v
        private static uint CompactBits(uint v)
        {
            v &= 0x55555555;
            v = (v ^ (v >> 1)) & 0x33333333;
            v = (v ^ (v >> 2)) & 0x0F0F0F0F;
            v = (v ^ (v >> 4)) & 0x00FF00FF;
            v = (v ^ (v >> 8)) & 0x0000FFFF;
            return v;
        }

        // Size of the mip chain in a DDS file (every level is w*h*bytesPerTexel, no minimum)
        public static int GetLinearChainSize(int width, int height, int numLevels, int bytesPerTexel)
        {
            int total = 0;
            int w = width, h = height;
            for (int i = 0; i < numLevels; i++)
            {
                total += w * h * bytesPerTexel;
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }
            return total;
        }

        // Size of the mip chain in PS3 texture data, not including the trailing zero padding that follows it
        public static int GetSwizzledChainSize(int width, int height, int numLevels, int bytesPerTexel)
        {
            int total = 0;
            int w = width, h = height;
            for (int i = 0; i < numLevels; i++)
            {
                total += Math.Max(MinLevelBytes, w * h * bytesPerTexel);
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }
            return total;
        }

        private static void DeswizzleLevel(byte[] src, int srcOffset, byte[] dst, int dstOffset, int size, int bytesPerTexel)
        {
            int count = size * size;
            for (int i = 0; i < count; i++)
            {
                int x = (int)CompactBits((uint)i);
                int y = (int)CompactBits((uint)i >> 1);
                Buffer.BlockCopy(src, srcOffset + i * bytesPerTexel, dst, dstOffset + (y * size + x) * bytesPerTexel, bytesPerTexel);
            }
        }

        private static void SwizzleLevel(byte[] src, int srcOffset, byte[] dst, int dstOffset, int size, int bytesPerTexel)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = (int)(SpreadBits((uint)x) | (SpreadBits((uint)y) << 1));
                    Buffer.BlockCopy(src, srcOffset + (y * size + x) * bytesPerTexel, dst, dstOffset + i * bytesPerTexel, bytesPerTexel);
                }
            }
        }

        // Converts PS3 swizzled texture data to a linear mip chain (DDS layout)
        public static byte[] DeswizzleMipChain(byte[] data, int width, int height, int numLevels, int bytesPerTexel)
        {
            if (data.Length < GetSwizzledChainSize(width, height, numLevels, bytesPerTexel))
            {
                throw new InvalidDataException("Texture data is smaller than its mip chain");
            }

            byte[] output = new byte[GetLinearChainSize(width, height, numLevels, bytesPerTexel)];
            int srcOffset = 0;
            int dstOffset = 0;
            int tailOffset = -1;
            int size = width;
            for (int i = 0; i < numLevels; i++)
            {
                int levelBytes = size * size * bytesPerTexel;
                if (levelBytes >= MinLevelBytes)
                {
                    DeswizzleLevel(data, srcOffset, output, dstOffset, size, bytesPerTexel);
                    srcOffset += levelBytes;
                }
                else
                {
                    // Tiny levels are packed back to back (a 2x2 level is the same in raster and Morton order)
                    if (tailOffset < 0) { tailOffset = srcOffset; }
                    Buffer.BlockCopy(data, tailOffset, output, dstOffset, levelBytes);
                    tailOffset += levelBytes;
                }
                dstOffset += levelBytes;
                size = Math.Max(1, size / 2);
            }
            return output;
        }

        // Converts a linear mip chain (DDS layout) to PS3 swizzled texture data
        public static byte[] SwizzleMipChain(byte[] linear, int width, int height, int numLevels, int bytesPerTexel)
        {
            if (linear.Length < GetLinearChainSize(width, height, numLevels, bytesPerTexel))
            {
                throw new InvalidDataException("Image data is smaller than its mip chain");
            }

            byte[] output = new byte[GetSwizzledChainSize(width, height, numLevels, bytesPerTexel)];
            int srcOffset = 0;
            int dstOffset = 0;
            int tailOffset = -1;
            int size = width;
            for (int i = 0; i < numLevels; i++)
            {
                int levelBytes = size * size * bytesPerTexel;
                if (levelBytes >= MinLevelBytes)
                {
                    SwizzleLevel(linear, srcOffset, output, dstOffset, size, bytesPerTexel);
                    dstOffset += levelBytes;
                }
                else
                {
                    if (tailOffset < 0) { tailOffset = dstOffset; }
                    Buffer.BlockCopy(linear, srcOffset, output, tailOffset, levelBytes);
                    tailOffset += levelBytes;
                }
                srcOffset += levelBytes;
                size = Math.Max(1, size / 2);
            }
            return output;
        }

        // Detects 8-bit single channel DDS files: L8, A8, or R8/A8 with a DX10 header.
        // They all hold the same data, they only differ in which channel(s) a viewer shows it in.
        // dataOffset is where the pixel data starts (128, or 148 with a DX10 header)
        public static bool TryGetSingleChannel8Format(byte[] dds, out int dataOffset)
        {
            const uint DDPF_ALPHA = 0x2, DDPF_FOURCC = 0x4, DDPF_RGB = 0x40, DDPF_LUMINANCE = 0x20000;
            const uint DXGI_R8_UNORM = 61, DXGI_A8_UNORM = 65;

            dataOffset = 128;
            if (dds.Length < 128) { return false; }

            uint flags = BitConverter.ToUInt32(dds, 80);
            uint bitCount = BitConverter.ToUInt32(dds, 88);
            uint rMask = BitConverter.ToUInt32(dds, 92);

            if ((flags & DDPF_FOURCC) != 0)
            {
                if (dds[84] == 'D' && dds[85] == 'X' && dds[86] == '1' && dds[87] == '0' && dds.Length >= 148)
                {
                    uint dxgiFormat = BitConverter.ToUInt32(dds, 128);
                    if (dxgiFormat == DXGI_R8_UNORM || dxgiFormat == DXGI_A8_UNORM)
                    {
                        dataOffset = 148;
                        return true;
                    }
                }
                return false;
            }

            if (bitCount != 8) { return false; }
            if ((flags & (DDPF_ALPHA | DDPF_LUMINANCE)) != 0 && (flags & DDPF_RGB) == 0) { return true; }
            return (flags & DDPF_RGB) != 0 && rMask == 0xFF; // Some tools write R8 as a one channel RGB format
        }
    }
}
