namespace Backrooms.Entities;

public abstract class EntityBase(Entity entity, Game game, SpriteRenderer sprRend, AudioSource audioSrc)
{
    public readonly Entity entity = entity;
    public readonly Game game = game;
    public readonly EntityTags tags = entity.tags;
    public readonly SpriteRenderer sprRend = sprRend;
    public readonly AudioSource audioSrc = audioSrc;
    public float audioMinDist = .65f, absFalloffBegin = 7.5f, absFalloffEnd = 10f;


    public Vec2f playerPos => game.camera.pos;
    public float playerAngle => game.camera.angle;
    public float playerDist => (playerPos - pos).length;
    public Vec2f pos
    {
        get => entity.pos;
        set => entity.pos = value;
    }
    public bool linOfSightToPlayer => game.map.LineOfSight(pos, playerPos);


    public virtual void Tick(float dt) { }
    public virtual void FixedTick(float fdt) { }
    public virtual void Pulse() { }
    public virtual void GenerateMap(Vec2f center) { }
    public virtual float GetVolume(float dist) => AudioRolloff.ForcedFalloff(dist, AudioRolloff.GetVolumeSqr(dist, audioMinDist), absFalloffBegin, absFalloffEnd);

    public abstract void Awake();
    public abstract void Destroy();
}