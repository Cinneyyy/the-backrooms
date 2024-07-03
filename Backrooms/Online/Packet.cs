using Backrooms.Serialization;

namespace Backrooms.Online;

public abstract class Packet<T>() : Serializable<T> where T : Packet<T>, new()
{
}