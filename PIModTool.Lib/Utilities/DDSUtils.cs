namespace PIModTool.Lib.Utilities
{
    // Byte positions of each channel within a 32-bit texel
    public readonly record struct ArgbLayout(int R, int G, int B, int A);
    public static class DDSUtils
    {

        // DDS A8R8G8B8 as stored in memory: B, G, R, A (a little-endian 0xAARRGGBB)
        public static readonly ArgbLayout NativeArgbLayout = new ArgbLayout(2, 1, 0, 3);

        private static int ByteIndexFromMask(uint mask)
        {
            switch (mask)
            {
                case 0x000000FF: return 0;
                case 0x0000FF00: return 1;
                case 0x00FF0000: return 2;
                case 0xFF000000: return 3;
                default: return -1; // Not a whole byte (e.g. A2R10G10B10)
            }
        }

        // Detects 32-bit, 8 bits per channel uncompressed DDS files in any channel order
        // (A8R8G8B8, A8B8G8R8, X8R8G8B8..., or the equivalent DXGI formats behind a DX10 header).
        // dataOffset is where the pixel data starts (128, or 148 with a DX10 header).
        // layout is the byte position of each channel in the file; A is -1 when the file has no alpha.
        public static bool TryGetArgb8888Format(byte[] dds, out int dataOffset, out ArgbLayout layout)
        {
            const uint DDPF_FOURCC = 0x4, DDPF_RGB = 0x40;

            dataOffset = 128;
            layout = NativeArgbLayout;
            if (dds.Length < 128) { return false; }

            uint flags = BitConverter.ToUInt32(dds, 80);

            if ((flags & DDPF_FOURCC) != 0)
            {
                if (!(dds[84] == 'D' && dds[85] == 'X' && dds[86] == '1' && dds[87] == '0' && dds.Length >= 148)) { return false; }

                switch (BitConverter.ToUInt32(dds, 128))
                {
                    case 27:
                    case 28:
                    case 29: // R8G8B8A8 typeless / UNORM / UNORM_SRGB
                        layout = new ArgbLayout(0, 1, 2, 3);
                        break;
                    case 87:
                    case 90:
                    case 91: // B8G8R8A8 UNORM / typeless / UNORM_SRGB
                        layout = new ArgbLayout(2, 1, 0, 3);
                        break;
                    case 88:
                    case 92:
                    case 93: // B8G8R8X8 UNORM / typeless / UNORM_SRGB (no alpha)
                        layout = new ArgbLayout(2, 1, 0, -1);
                        break;
                    default:
                        return false;
                }
                dataOffset = 148;
                return true;
            }

            if ((flags & DDPF_RGB) == 0 || BitConverter.ToUInt32(dds, 88) != 32) { return false; }

            int r = ByteIndexFromMask(BitConverter.ToUInt32(dds, 92));
            int g = ByteIndexFromMask(BitConverter.ToUInt32(dds, 96));
            int b = ByteIndexFromMask(BitConverter.ToUInt32(dds, 100));
            uint aMask = BitConverter.ToUInt32(dds, 104);
            int a = aMask == 0 ? -1 : ByteIndexFromMask(aMask);
            if (r < 0 || g < 0 || b < 0 || (aMask != 0 && a < 0)) { return false; }

            // Every channel must come from a different byte
            int used = (1 << r) | (1 << g) | (1 << b);
            if ((1 << r) + (1 << g) + (1 << b) != used || (a >= 0 && (used & (1 << a)) != 0)) { return false; }

            layout = new ArgbLayout(r, g, b, a);
            return true;
        }

        // Rewrites 32-bit texel data in place from the given channel layout to B, G, R, A.
        // A file without alpha gets an opaque alpha channel.
        public static void ConvertToBgra(byte[] data, ArgbLayout layout)
        {
            if (layout == NativeArgbLayout) { return; }

            for (int i = 0; i + 3 < data.Length; i += 4)
            {
                byte b = data[i + layout.B];
                byte g = data[i + layout.G];
                byte r = data[i + layout.R];
                byte a = layout.A >= 0 ? data[i + layout.A] : (byte)255;
                data[i] = b;
                data[i + 1] = g;
                data[i + 2] = r;
                data[i + 3] = a;
            }
        }
    }
}
