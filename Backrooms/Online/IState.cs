using System;

namespace Backrooms.Online;

public interface IState<TKey> where TKey : Enum
{
    void Deserialize(byte[] data, int start, int end);
    byte[] Serialize(byte? clientData = null, params TKey[] dataKeys);
}