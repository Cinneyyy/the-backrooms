using Backrooms.Serialization;

namespace Backrooms.OnlineNew;

public abstract class Packet<T>() : Serializable<T> where T : Packet<T>, new()
{
}