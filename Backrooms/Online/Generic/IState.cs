using System;

namespace Backrooms.Online.Generic;

public interface IState<TKey> where TKey : Enum
{
    byte[] Serialize(params TKey[] dataKeys);
    void Deserialize(byte[] data, int start, int? length, out int bytesRead);
}