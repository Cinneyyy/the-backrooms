using NAudio.Wave;

namespace Backrooms;

public class LoopingWaveStream(WaveStream sourceStream, bool loop) : WaveStream
{
    public bool loop = loop;

    private readonly WaveStream sourceStream = sourceStream;


    public override WaveFormat WaveFormat => sourceStream.WaveFormat;
    public override long Length => sourceStream.Length;
    public override long Position
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while(totalBytesRead < count)
        {
            int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

            if(bytesRead == 0)
            {
                if(sourceStream.Position == 0 || !loop)
                    break;

                sourceStream.Position = 0;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    public new void Dispose()
    {
        base.Dispose();
        sourceStream.Dispose();
    }
}