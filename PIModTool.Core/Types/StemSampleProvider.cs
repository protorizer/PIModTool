using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// A wrapper on ISampleProvider that lets you swap the stream it reads from without interruption
public class StemSampleProvider: ISampleProvider
{
    private ISampleProvider _sampleProvider;

    public StemSampleProvider(ISampleProvider stemProvider)
    {
        _sampleProvider = stemProvider;
        WaveFormat = stemProvider.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public void ChangeStem(ISampleProvider newStem)
    {
        if (!newStem.WaveFormat.Equals(WaveFormat))
        {
            throw new InvalidOperationException("WaveFormat of new stream does not match that of the old stream.");
        }
        Interlocked.Exchange(ref _sampleProvider, newStem);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return _sampleProvider.Read(buffer, offset, count);
    }
}
