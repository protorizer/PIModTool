using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIModTool.Lib
{
    public static class YukHandler
    {
        // Deinterlace a yuk into its 8 constituent streams
        // Input: FileStream of a .yuk file
        public static MemoryStream[] Deinterlace(FileStream file)
        {
            MemoryStream[] streams = new MemoryStream[8];
            const int chunkSize = 0x30000; // Chunks of 0x6000 per stream, and 8 streams

            long yukLength = file.Length;
            int numChunks = (int)yukLength / chunkSize;
            int numExtraChunks = 0, numExtraChunks2 = 0; // Used for writing small sections of audio smaller than a full chunk

            // Check for extra (small) chunks after the main ones
            if (yukLength % chunkSize != 0)
            {
                numChunks--;
                int remainingBytes = (int)yukLength - (numChunks * chunkSize);
                int remainingChunks = remainingBytes / 384 / 8; // 384 bytes per mini chunk
                numExtraChunks = remainingChunks / 2;
                numExtraChunks2 = remainingChunks - numExtraChunks;
            }

            byte[] stemBuffer = new byte[chunkSize / 8];

            // Initialize memorystreams
            for (int i = 0; i < 8; i++)
            {
                streams[i] = new MemoryStream();
            }

            // Write main chunks
            for (int i = 0; i < numChunks; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    file.Read(stemBuffer, 0, 0x6000);
                    streams[j].Write(stemBuffer, 0, 0x6000);
                }
            }

            // Write first set of extra chunks
            for (int i = 0; i < 8; i++)
            {
                file.Read(stemBuffer, 0, numExtraChunks * 384);
                streams[i].Write(stemBuffer, 0, numExtraChunks * 384);
            }

            // Write second set of extra chunks
            for (int i = 0; i < 8; i++)
            {
                file.Read(stemBuffer, 0, numExtraChunks2 * 384);
                streams[i].Write(stemBuffer, 0, numExtraChunks2 * 384);
            }

            return streams;
        }

        // Interlaces 8 atrac3 streams into a .yuk file
        public static MemoryStream Interlace(MemoryStream[] streams)
        {
            if (streams.Length != 8)
            {
                throw new ArgumentException(".yuk file requires exactly 8 streams");
            }

            const int chunkSize = 0x30000; // Chunks of 0x6000 per stream, and 8 streams

            long yukLength = streams[0].Length * 8;
            int numChunks = (int)yukLength / chunkSize;
            int numExtraChunks = 0, numExtraChunks2 = 0; // Used for writing small sections of audio smaller than a full chunk

            // Check for extra (small) chunks after the main ones
            if (yukLength % chunkSize != 0)
            {
                numChunks--;
                int remainingBytes = (int)yukLength - (numChunks * chunkSize);
                int remainingChunks = remainingBytes / 384 / 8; // 384 bytes per mini chunk
                numExtraChunks = remainingChunks / 2;
                numExtraChunks2 = remainingChunks - numExtraChunks;
            }

            byte[] stemBuffer = new byte[chunkSize / 8];
            MemoryStream yukStream = new MemoryStream();

            // Write main chunks
            for (int i = 0; i < numChunks; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int numRead = streams[j].Read(stemBuffer, 0, 0x6000);
                    if (numRead != 0x6000)
                    {
                        throw new InvalidDataException("Unexpected end of stream");
                    }
                    yukStream.Write(stemBuffer, 0, 0x6000);
                }
            }

            // Write first set of extra chunks
            for (int i = 0; i < 8; i++)
            {
                int numRead = streams[i].Read(stemBuffer, 0, numExtraChunks * 384);
                if (numRead != numExtraChunks * 384)
                {
                    throw new InvalidDataException("Unexpected end of stream");
                }
                yukStream.Write(stemBuffer, 0, numExtraChunks * 384);
            }

            // Write second set of extra chunks
            for (int i = 0; i < 8; i++)
            {
                int numRead = streams[i].Read(stemBuffer, 0, numExtraChunks2 * 384);
                if (numRead != numExtraChunks2 * 384)
                {
                    throw new InvalidDataException("Unexpected end of stream");
                }
                yukStream.Write(stemBuffer, 0, numExtraChunks2 * 384);
            }

            return yukStream;
        }

        // Uses vgmstream to convert an atrac stream to a wav stream
        // Writes the atrac and wav to the system temp directories, and returns the wav as a byte[]
        // TODO: In the future, add error codes to this function to allow for more detailed error messages
        public static async Task<byte[]?> ConvertToWav(byte[] data)
        {
            string vgmstreamPath = Path.Combine(AppContext.BaseDirectory, "tools", "vgmstream-cli", "vgmstream-cli.exe");
            if (!Path.Exists(vgmstreamPath))
            {
                // Could not find vgmstream
                return null;
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), "PIModTool", Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempFilePath);

                // Write atrac stream to a temporary file
                FileStream atracStream = new FileStream(Path.Combine(tempFilePath, "tmpstream.atrac"), FileMode.Create);
                atracStream.Write(data, 0, data.Length);
                atracStream.Close();

                // Write vgmstream's atrac header data
                FileStream headerStream = new FileStream(Path.Combine(tempFilePath, ".atrac.txth"), FileMode.Create);
                byte[] headerInfo = new UTF8Encoding(true).GetBytes("codec = ATRAC3\nsample_rate = 48000\nchannels = 2\nstart_offset = 0\ninterleave = 0x180\nnum_samples = data_size");
                headerStream.Write(headerInfo, 0, headerInfo.Length);
                headerStream.Close();

                // Run vgmstream
                ProcessStartInfo vgmInfo = new ProcessStartInfo
                {
                    FileName = vgmstreamPath,
                    Arguments = "-o tmpwav.wav tmpstream.atrac",
                    WorkingDirectory = tempFilePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process vgmProc = new Process { StartInfo = vgmInfo };
                vgmProc.Start();

                await vgmProc.WaitForExitAsync();

                FileStream wavStream = File.OpenRead(Path.Combine(tempFilePath, "tmpwav.wav"));
                byte[] wavData = new byte[wavStream.Length];
                wavStream.Read(wavData, 0, wavData.Length);
                wavStream.Close();

                return wavData;
            }
            catch
            {
                // Some sort of error happened
                return null;
            }
            finally
            {
                // Delete temporary files
                Directory.Delete(tempFilePath, true);
            }
        }

        // Uses at3tool to convert a wav stream to an atrac stream
        // Writes the atrac and wav to the system temp directories, and returns the atrac as a byte[]
        // TODO: In the future, add error codes to this function to allow for more detailed error messages
        public static async Task<byte[]?> ConvertToAtrac(byte[] data)
        {
            string at3toolPath = Path.Combine(AppContext.BaseDirectory, "tools", "at3tool", "ps3_at3tool.exe");
            if (!Path.Exists(at3toolPath))
            {
                return null;
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), "PIModTool", Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempFilePath);

                // Write wav to a temporary file
                FileStream wavStream = new FileStream(Path.Combine(tempFilePath, "tmpstream.wav"), FileMode.Create);
                wavStream.Write(data, 0, data.Length);
                wavStream.Close();

                // Run at3tool
                ProcessStartInfo at3Info = new ProcessStartInfo
                {
                    FileName = at3toolPath,
                    Arguments = "-br 144 -e tmpstream.wav tmpstream.atrac",
                    WorkingDirectory = tempFilePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process at3Proc = new Process { StartInfo = at3Info };
                at3Proc.Start();

                await at3Proc.WaitForExitAsync();

                FileStream at3Stream = File.OpenRead(Path.Combine(tempFilePath, "tmpstream.atrac"));
                byte[] at3Data = new byte[at3Stream.Length];
                at3Stream.Read(at3Data, 0, at3Data.Length);
                at3Stream.Close();

                return at3Data;
            }
            catch
            {
                // Some error happened
                return null;
            }
            finally
            {
                // Delete temporary files
                Directory.Delete(tempFilePath, true);
            }
        }
    }
}
