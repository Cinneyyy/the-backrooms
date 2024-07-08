using System;
using Backrooms;
using Backrooms.Debug;
using Backrooms.Entities;

public class Behaviour(Entity entity, Game game, SpriteRenderer sprRend, AudioSource audioSrc) : EntityBase(entity, game, sprRend, audioSrc)
{
    public bool woke = false;


    public override void Awake()
    {
        woke = true;
        maxAudioDist = 7.5f;
        sprRend.enabled = true;

        pos = playerPos;
        game.window.pulse += () => entity.managedPathfinding.FindPath(pos, playerPos);
    }

    public override void Tick(float dt)
    {
        if(!woke)
            return;

        Logger.Out($"PlayerPos: {playerPos} ;; EntityPos: {pos}");
        sprRend.pos = pos;
    }
}