using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// A wrapper on WaveStream that automatically loops playback when reaching the end
// Has a looping toggle that is disabled for exporting
public class LoopingWaveStream: WaveStream
{
    private readonly WaveStream _sourceStream;
    private bool _loop = true;

    public LoopingWaveStream(WaveStream sourceStream)
    {
        _sourceStream = sourceStream;
    }

    public override long Length
    {
        get { return _sourceStream.Length; }
    }
    public override long Position
    {
        get { return _sourceStream.Position; }
        set { _sourceStream.Position = value; }
    }

    public bool Loop
    {
        get { return _loop; }
        set { _loop = value; }
    }

    public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (Loop)
                {
                    _sourceStream.Position = 0;
                }
                else
                {
                    break;
                }
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }
}
