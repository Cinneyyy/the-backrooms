using System.IO.Compression;

namespace Backrooms.Online;

public class CommonState(int bufSize = 1024, bool logPackets = false, CompressionLevel packetCompression = CompressionLevel.Optimal)
{
    public readonly int bufSize = bufSize;
    public readonly CompressionLevel packetCompression = packetCompression;
    public readonly bool decompress = packetCompression != CompressionLevel.NoCompression;
    public bool logPackets = logPackets;
}