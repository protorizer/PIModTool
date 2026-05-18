namespace PIModTool.Lib.Types
{
    // Texture format is stored as a 4 byte value in the texture header - it seems like X360 uses the values from the XDK for D3D texture formats directly
    // Meanwhile PS3 reuses the same 4 byte section, but has simple values for each texture type. We can use this difference to differentiate platform
    // The X360 hex values here are taken directly from disassembled X360 code
    public enum DDSType: UInt32
    {
        PS3_DXT1 = 1,
        PS3_DXT5 = 5,
        X360_DXT1 = 0x1A200152,
        X360_DXN = 0x1A200171, // ATI2
        X360_A8R8G8B8 = 0x18280186, // Unsure what this is for but it's referenced in the code
        X360_L8 = 0x28000102, // Unsure what this is for but it's referenced in the code
        X360_DXT5 = 0x1A200154, // Shared with DXT4
    };

    public static class DDSTypeExtensions
    {
        // IDK a cleaner way to do this
        public static bool IsEqual(this DDSType a, DDSType b)
        {
            // DXT1 formats are equal
            if((a == DDSType.PS3_DXT1 && b == DDSType.X360_DXT1) || (a == DDSType.X360_DXT1 && b == DDSType.PS3_DXT1))
            {
                return true;
            }

            // DXT5 formats are equal
            if ((a == DDSType.PS3_DXT5 && b == DDSType.X360_DXT5) || (a == DDSType.X360_DXT5 && b == DDSType.PS3_DXT5))
            {
                return true;
            }

            return a == b;
        }
    }

    public enum TextureType: byte
    {
        TYPE_2D = 0,
        TYPE_CUBE = 1,
        TYPE_VOLUME = 2
    };

    // Exact representation of the 32 byte X2M/P3M texture header
    public struct PITexture
    {
        public UInt16 Width; // Normal texture width
        public UInt16 Height; // Normal texture height
        public UInt16 HDWidth; // .pit texture width
        public UInt16 HDHeight; // .pit texture height
        public UInt16 Padding; // 2 bytes of 00 padding
        public byte VolumeTextureDepth; // If not a volume texture, set to 1
        public byte HDTextureDepth; // .pit volume texture depth
        public byte MipmapCount; // Mipmap count of normal texture
        public byte HDMipmapCount; // Mipmap count of .pit texture
        public TextureType Type; // Type of texture, see TextureType enum
        public byte StreamPriority; // Higher numbers get their .pit verion loaded sooner, default is 1
        public DDSType TextureFormat; // Platform & compression format
        public UInt32 DataSize; // Size of the normal texture data
        public UInt32 HDDataSize; // Size of the .pit texture data
        public UInt32 HDStreamOffset; // Offset into the .pit file the HD texture is located
        public byte[] Data; // normal texture data
    }
}
