using System;
using Backrooms;
using Backrooms.Debugging;
using Backrooms.Entities;

public class Behaviour(EntityManager manager, EntityType type) : EntityInstance(manager, type)
{
    public override void Awake()
    {
        sprRend.enabled = true;
        audioSrc.Play();
        pos = playerPos;
    }

    public override void GenerateMap(Vec2f center)
        => pos = center;

    public override void Tick(float dt)
        => Logger.Out(Logger.Log.Entity, pos);

    public override void Destroy() { }
}