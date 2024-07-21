using System;
using Backrooms;
using Backrooms.Debugging;
using Backrooms.Entities;

public class Behaviour(Entity entity, Game game, SpriteRenderer sprRend, AudioSource audioSrc) : EntityBase(entity, game, sprRend, audioSrc)
{
    public override void Awake()
    {
        sprRend.enabled = true;
        audioSrc.Play();
        pos = playerPos;
    }

    public override void GenerateMap(Vec2f center)
        => pos = center;

    public override void Pulse()
        => entity.managedPathfinding.FindPath(pos, playerPos);

    public override void Destroy() { }
}