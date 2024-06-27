using System.IO.Compression;

namespace Backrooms.OnlineNew.Generic;

public class CommonState(int bufSize = 1024, bool printDebug = false, CompressionLevel packetCompression = CompressionLevel.Optimal)
{
    public readonly int bufSize = bufSize;
    public bool printDebug = printDebug;
    public CompressionLevel packetCompression = packetCompression;
}