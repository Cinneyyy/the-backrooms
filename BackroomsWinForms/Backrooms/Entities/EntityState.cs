using Backrooms.Serialization;

namespace Backrooms.Entities;

public class EntityState() : Serializable<EntityState>()
{
    public Vec2f pos;
    public ushort target;
}