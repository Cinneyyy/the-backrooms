using System;
using Backrooms;
using Backrooms.Serialization;
using Backrooms.Debugging;
using Backrooms.Entities;

namespace Olaf;

public class Behaviour(EntityManager manager, EntityType type) : EntityInstance(manager, type)
{
    public override void Awake()
    {
        sprRend.enabled = true;
        audioPlayback.Play();
        pos = playerPos;
    }

    public override void Pulse()
    {
        Logger.Out(Logger.Log.Debug, pos.ToString());
    }

    public override void Tick(float dt)
    {
        sprRend.pos = pos;
    }

    public override void GenerateMap(Vec2f center)
        => pos = center;

    public override void Destroy() { }
}