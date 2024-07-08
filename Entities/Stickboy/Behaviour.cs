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
        sprRend.enabled = true;
        audioSrc.Play();
        pos = playerPos;
        game.window.pulse += () => entity.managedPathfinding.FindPath(pos, playerPos);
    }
}