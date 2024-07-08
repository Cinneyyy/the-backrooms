using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Backrooms.Pathfinding;
using IO = System.IO;

namespace Backrooms.Entities;

public class Entity
{
    public Vec2f pos;
    public readonly EntityTags tags;
    public readonly Assembly behaviourAsm;
    public readonly Type behaviourType;
    public readonly EntityBase instance;
    public readonly PathfindingEntity managedPathfinding;


    public Entity(EntityManager manager, string dataPath)
    {
        try
        {
            // Tags
            tags = JsonSerializer.Deserialize<EntityTags>(File.ReadAllText($"{dataPath}/tags.json"));

            // Sprite & audio (only loaded if sprite/audio is not null respectively)
            UnsafeGraphic sprite = new(tags.sprite is null ? Resources.sprites["empty"] : Image.FromFile(tags.sprite));
            SpriteRenderer sprRend = new(Vec2f.zero, tags.size, sprite) {
                enabled = false
            };
            manager.rend.sprites.Add(sprRend);
            AudioSource audioSrc = tags.audio is null ? new(Resources.audios["silence"], true) : new(tags.audio, true);
            audioSrc.volume = 0f;

            // Find source files
            IEnumerable<string> srcFiles = from f in Directory.GetFiles(dataPath, "*.cs", SearchOption.AllDirectories)
                                           select File.ReadAllText(f);

            // Compile behaviour code
            Type baseType = typeof(EntityBase);
            behaviourAsm = CsCompiler.BuildAssembly([..srcFiles], IO::Path.GetFileNameWithoutExtension(dataPath).Replace(' ', '-').ToLower());
            behaviourType = behaviourAsm.GetType(tags.instance);
            if(!behaviourType.IsAssignableTo(baseType))
                throw new($"The behaviour instance type must be derived from EntityBase");

            // Instantiate instance
            object[] instanceArgs = [/*entity*/this, /*game*/manager.game, /*sprRend*/sprRend, /*audioSrc*/audioSrc];
            instance = Activator.CreateInstance(behaviourType, instanceArgs) as EntityBase;

            // Add callbacks to overriden methods
            bool isOverriden(string name) 
                => behaviourType.GetMethod(name).DeclaringType != baseType;
            if(isOverriden(nameof(EntityBase.Tick))) manager.entityTick += instance.Tick;
            if(isOverriden(nameof(EntityBase.FixedTick))) manager.fixedEntityTick += instance.FixedTick;
            if(isOverriden(nameof(EntityBase.Awake))) manager.entityActivate += instance.Awake;
            if(isOverriden(nameof(EntityBase.Pulse))) manager.entityPulse += instance.Pulse;
            if(isOverriden(nameof(EntityBase.GenerateMap))) manager.game.generateMap += instance.GenerateMap;

            // Initiate pathfinding, if managed
            if(tags.managedPathfinding is EntityTags.ManagedPathfinding pathfindingData)
            {
                IEnumerable<Type> algorithmTypes = from asm in AppDomain.CurrentDomain.GetAssemblies()
                                                   let pType = asm.GetType(pathfindingData.algorithmName)
                                                   where pType is not null
                                                   select pType;

                if(algorithmTypes.Count() > 1) 
                    throw new($"Found more than one ({algorithmTypes.Count()}) types matching the type name '{pathfindingData.algorithmName}'");
                if(!algorithmTypes.Any())
                    throw new($"Found no suitable type with the name '{pathfindingData.algorithmName}'");

                managedPathfinding = new(manager.map, Activator.CreateInstance(algorithmTypes.First()) as IPathfindingAlgorithm);
                manager.entityTick += dt => pos = managedPathfinding.MoveTowards(pos, tags.size.x/2f, pathfindingData.speed, dt);
            }

            Out($"Successfully loaded entity at {dataPath}");
        }
        catch(Exception exc)
        {
            OutErr(exc, $"There was an error loading entity at {dataPath}: $e", ConsoleColor.Red);
        }
    }
}