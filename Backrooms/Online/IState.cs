using System;

namespace Backrooms.Online;

public interface IState<TKey> where TKey : Enum
{
    void Deserialize(byte[] data, int start, int length);
    byte[] Serialize(TKey[] dataKeys);
}