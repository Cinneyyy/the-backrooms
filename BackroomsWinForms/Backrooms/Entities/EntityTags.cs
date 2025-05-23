﻿namespace Backrooms.Entities;

public readonly record struct EntityTags
{
    public readonly record struct Vector2(float x, float y)
    {
        public static implicit operator Vec2f(Vector2 v) => new(v.x, v.y);
    }

    public readonly record struct ManagedContactDamage(float damage, float cooldown, float maxDist);


    public string instance { get; init; }
    public string pathfindingAlgorithm { get; init; }
    public float? managedPathfindingSpeed { get; init; }
    public ManagedContactDamage? managedContactDamage { get; init; }
    public Vector2 size { get; init; }
    public float? elevation { get; init; }
    public string sprite { get; init; }
    public string audio { get; init; }
    public bool manageSprRendPos { get; init; }
    public bool manageAudioVol { get; init; }
    public bool manageAudioPan { get; init; }
    public float audioPadding { get; init; }
}