using System.Text;
using System.Text.RegularExpressions;
using PIModTool.Lib.Types;

namespace PIModTool.Lib
{
    public static class FileTypeHandler
    {
        static readonly byte[] DDSHeader = [0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00];
        public static FileType DetectFileType(byte[] fileContent)
        {
            if (fileContent.Length > DDSHeader.Length && fileContent.Take(DDSHeader.Length).SequenceEqual(DDSHeader))
            {
                return FileType.DDS;
            }
            // Insert other binary types here

            string textContent;
            try
            {
                textContent = Encoding.UTF8.GetString(fileContent);
            }
            catch
            {
                return FileType.UnknownBinary;
            }

            string[] lines = textContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Matches elements unique to each format and will identify based on which one there's the most of
            int numXml = 0, numObject = 0, numEvent = 0, numPSC = 0;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                if (trimmed.StartsWith("<?xml"))
                {
                    numXml++;
                }

                if (trimmed.Contains("{") || trimmed.Contains("};"))
                {
                    numObject++;
                }

                if (trimmed.StartsWith("Begin ") || trimmed.StartsWith("End")){
                    numEvent++;
                }

                if (Regex.IsMatch(trimmed, @"^[A-Za-z_][A-Za-z0-9_]*\s+[0-9]+(?:\.[0-9]+)?(?:\s*\/\/.*)?$"))
                {
                    numPSC++;
                }
            }

            int[] nums = { numXml, numObject, numEvent, numPSC };
            int max = nums.Max();
            if (max == 0)
            {
                return FileType.UnknownText;
            }
            else if (max == numXml)
            {
                return FileType.XML;
            }
            else if (max == numObject)
            {
                return FileType.ObjectScript;
            }
            else if (max == numEvent)
            {
                return FileType.EventScript;
            }
            else if (max == numPSC)
            {
                return FileType.PSCScript;
            }
            else
            {
                return FileType.Unknown;
            }
        }
    }
}
