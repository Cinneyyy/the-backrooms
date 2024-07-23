using System.Linq;
using NAudio.Wave.SampleProviders;
using Backrooms.Pathfinding;

namespace Backrooms.Entities;

public abstract class EntityInstance
{
    public Vec2f pos;
    public readonly EntityManager manager;
    public readonly EntityType type;
    public readonly EntityTags tags;
    public readonly SpriteRenderer sprRend;
    public readonly AudioSource audioSrc;
    public float audioMinDist = .65f, absFalloffBegin = 7.5f, absFalloffEnd = 10f;
    public Path currPath = new(pointArr: []);


    public Vec2f playerPos => manager.camera.pos;
    public float playerAngle => manager.camera.angle;
    public float playerDist => (playerPos - pos).length;
    public bool linOfSightToPlayer => manager.map.LineOfSight(pos, playerPos);


    public EntityInstance(EntityManager manager, EntityType type)
    {
        this.manager = manager;
        this.type = type;
        tags = type.tags;

        sprRend = new(Vec2f.zero, tags.size, type.providedSprite) {
            enabled = false,
            elevation = tags.elevation
        };
        manager.rend.sprites.Add(sprRend);

        audioSrc = new(type.providedAudio, true) {
            disposeStream = false,
            volume = 0f,
            panStrategy = new SinPanStrategy()
        };

        if(type.implementedCallbacks.Contains(nameof(Tick))) manager.window.tick += Tick;
        if(type.implementedCallbacks.Contains(nameof(FixedTick))) manager.window.fixedTick += FixedTick;
        if(type.implementedCallbacks.Contains(nameof(Pulse))) manager.window.pulse += Pulse;
        if(type.implementedCallbacks.Contains(nameof(GenerateMap))) manager.game.generateMap += GenerateMap;

        if(tags.manageSprRendPos) manager.window.tick += dt => sprRend.pos = pos;
        if(tags.manageAudioVol) manager.window.tick += dt => audioSrc.volume = GetVolume(playerDist);
        if(tags.manageAudioPan) manager.window.tick += dt => audioSrc.panning = Vec2f.Pan(pos, playerPos, playerAngle);

        if(type.pathfinding is not null)
        {
            manager.window.pulse += () => currPath = type.pathfinding.FindPath(manager.map, pos.Floor(), playerPos.Floor());
            manager.window.tick += dt => {
                if(currPath.points is { Length: > 0 })
                    pos += (currPath.GetNextPoint(pos, .5f - tags.size.x/2f) - pos).normalized * tags.managedPathfinding.Value.speed * dt;
            };
        }

        if(manager.mpManager.isConnected)
            Awake();
        else
            manager.mpManager.connectedToServer += Awake;
    }


    public virtual void Tick(float dt) { }
    public virtual void FixedTick(float fdt) { }
    public virtual void Pulse() { }
    public virtual void GenerateMap(Vec2f center) { }
    public virtual float GetVolume(float dist)
        => AudioRolloff.ForcedFalloff(dist, AudioRolloff.GetVolumeSqr(dist, audioMinDist), absFalloffBegin, absFalloffEnd);

    public abstract void Awake();
    public abstract void Destroy();
}