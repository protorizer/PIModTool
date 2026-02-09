
using System.Text;

namespace PIModTool.Lib.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            StringBuilder sb = new StringBuilder();
            char character;

            while ((character = reader.ReadChar()) != default)
            {
                sb.Append(character);
            }

            return sb.ToString();
        }
    }
}
