namespace Backrooms.Entities;

public abstract class EntityBase(Entity entity, Game game, SpriteRenderer sprRend, AudioSource audioSrc)
{
    public readonly Entity entity = entity;
    public readonly Game game = game;
    public readonly EntityTags tags = entity.tags;
    public readonly SpriteRenderer sprRend = sprRend;
    public readonly AudioSource audioSrc = audioSrc;
    public float maxAudioDist;


    public Vec2f playerPos => game.camera.pos;
    public float playerAngle => game.camera.angle;
    public Vec2f pos
    {
        get => entity.pos;
        set => entity.pos = value;
    }


    public virtual void Tick(float dt) { }
    public virtual void FixedTick(float fdt) { }
    public virtual void Pulse() { }
    public virtual void Awake() { }
    public virtual float GetVolume(float dist) => 1f - dist/maxAudioDist;
    public virtual void GenerateMap(Vec2f center) { }
}