using System.IO;
using System.IO.Compression;

namespace Backrooms.Serialization;

public static class BinarySerializer
{
    public static byte[] Serialize<T>(T instance, CompressionLevel compression = CompressionLevel.Optimal) where T :
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
    }
}