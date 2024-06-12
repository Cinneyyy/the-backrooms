using System;
using Backrooms;

public class Behaviour(Game game, Entity entity)
{
    public Game game = game;
    public Entity entity = entity;
    public EntityTags tags = entity.tags;
    public bool awoken = false;
    public SpriteRenderer spriteRend;


    public void Tick(float dt)
    {
        if(!awoken)
            return;

        spriteRend.pos = entity.pos;
    }

    public void Awake()
    {
        awoken = true;

        spriteRend = new(entity.pos, new(tags.size), entity.sprite);
        game.renderer.sprites.Add(spriteRend);
        entity.pos = game.camera.pos;

        game.window.pulse += () => entity.pathfinding.FindPath(entity.pos, game.camera.pos);
    }

    public float GetVolume(float dist)
        => 1f - dist/7.5f;
}