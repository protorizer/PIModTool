
using NAudio.Wave;

namespace PIModTool.Core.Utilities
{
    public struct WaveformPeak
    {
        public float Min;
        public float Max;
    }
    public static class WaveformUtilities
    {

        // Calculate the peaks of a PCM stream's waveform for visualization.
        // targetWidth: Width in pixels that the displayed waveform will be. Used to optimize calculations by not computing more peaks than visible.
        public static WaveformPeak[] CalculatePeaks(WaveStream stream, int targetWidth)
        {
            int bytesPerSample = stream.WaveFormat.BitsPerSample / 8;
            int numChannels = stream.WaveFormat.Channels;
            long numSamples = stream.Length / bytesPerSample / numChannels;
            long samplesPerPeak = Math.Max(numSamples / targetWidth, 1);

            WaveformPeak[] peaks = new WaveformPeak[targetWidth];

            stream.Position = 0;

            int blockAlign = stream.WaveFormat.BlockAlign;

            // Compute how many bytes to read per peak
            long bytesPerPeak = samplesPerPeak * blockAlign;

            // Round up to multiple of blockAlign
            bytesPerPeak = ((bytesPerPeak + blockAlign - 1) / blockAlign) * blockAlign;
            byte[] buffer = new byte[bytesPerPeak];
            for (int i = 0; i < targetWidth; i++)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if(bytesRead == 0) { break; }

                float min = float.MaxValue;
                float max = float.MinValue;

                // Read in (1/targetWidth)th of the file at a time. Calculate the min/max of that section. Create the peaks based on that.
                for(int j = 0; j < bytesRead; j += bytesPerSample * numChannels)
                {
                    // Downmix waveforms to mono
                    float sample = 0f;
                    for(int channel = 0; channel < numChannels; channel++)
                    {
                        // Byte corresponding to the channel for the same point in time
                        int offset = j + channel * bytesPerSample;

                        float value = bytesPerSample switch
                        {
                            // If 2 bytes per sample, convert to a normalized float (PCM16 stores sample values as int16s)
                            2 => BitConverter.ToInt16(buffer, offset) / 32768f,
                            // If 4 bytes per sample, just read as single-precision float
                            4 => BitConverter.ToSingle(buffer, offset),
                            // No support for this just ignore it
                            _ => 0
                        };

                        sample += value;
                    }
                    sample = sample / numChannels;

                    if(sample < min) { min = sample; }
                    if(sample > max) { max = sample; }
                }

                peaks[i] = new WaveformPeak { Max = max, Min = min };
            }

            // Normalize all peaks to be on a 0-1 scale
            float minPeak = peaks.Min(p => Math.Min(Math.Abs(p.Min), p.Max));
            float maxPeak = peaks.Max(p => Math.Max(Math.Abs(p.Min), p.Max));

            for(int i = 0; i < peaks.Length; i++)
            {
                peaks[i] = new WaveformPeak
                {
                    Max = ((peaks[i].Max - minPeak) / (maxPeak - minPeak)) * 0.7f,
                    Min = ((peaks[i].Min + minPeak) / (maxPeak - minPeak)) * 0.7f
                };
            }

            return peaks;
        }
    }
}
