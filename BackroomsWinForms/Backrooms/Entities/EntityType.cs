﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Backrooms.Pathfinding;
using IO = System.IO;

namespace Backrooms.Entities;

public class EntityType
{
    public readonly EntityTags tags;
    public readonly Assembly behaviourAsm;
    public readonly Type behaviourType;
    public readonly Image providedSprite;
    public readonly LoopingWaveStream providedAudio;
    public readonly string[] implementedCallbacks;
    public readonly IPathfindingAlgorithm pathfinding;
    public readonly EntityManager manager;


    public EntityType(EntityManager manager, string dataPath)
    {
        this.manager = manager;

        try
        {
            // Load provided files
            tags = JsonSerializer.Deserialize<EntityTags>(File.ReadAllText($"{dataPath}/tags.json"));
            providedSprite = tags.sprite is null ? Resources.sprites["empty"] : Image.FromFile($"{dataPath}/{tags.sprite}");
            providedAudio = new(tags.audio is null ? Resources.audios["silence"] : Utils.FileToWaveStream($"{dataPath}/{tags.audio}"), true);

            // Compile behaviour
            IEnumerable<string> srcFiles = Directory.GetFiles(dataPath, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText);
            string asmName = IO::Path.GetFileNameWithoutExtension(dataPath).Replace(' ', '-').ToLowerInvariant();
            behaviourAsm = CsCompiler.BuildAssembly([..srcFiles], asmName, dataPath, out bool usedCachedDll);

            Out(Log.Entity, (usedCachedDll ? "Loaded entity code from " : "Compiled entity code to ") + $"{dataPath}{(IO::Path.DirectorySeparatorChar)}{asmName}.dll");

            behaviourType = behaviourAsm.GetType(tags.instance);
            if(!behaviourType.IsAssignableTo(typeof(EntityInstance)))
                throw new($"The behaviour instance type must be derived from EntityBase");

            // Get callbacks
            implementedCallbacks = typeof(EntityInstance).GetMethods()
                .Where(m => !m.IsAbstract && m.IsVirtual)
                .Where(m => behaviourType.GetMethod(m.Name)?.DeclaringType == behaviourType)
                .Select(m => m.Name)
                .ToArray();

            // Load pathfinding algorithm
            if(tags.pathfindingAlgorithm is not null)
            {
                IEnumerable<Type> algorithmTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(asm => asm.GetType(tags.pathfindingAlgorithm))
                    .Where(t => t is not null);

                if(algorithmTypes.Count() > 1)
                    throw new($"Found more than one ({algorithmTypes.Count()}) types matching the type name '{tags.pathfindingAlgorithm}'");
                if(!algorithmTypes.Any())
                    throw new($"Found no suitable type with the name '{tags.pathfindingAlgorithm}'");

                pathfinding = Activator.CreateInstance(algorithmTypes.Single()) as IPathfindingAlgorithm;
            }
            else
                pathfinding = null;

            Out(Log.Entity, $"Successfully loaded entity at {dataPath}");
        }
        catch(Exception exc)
        {
            OutErr(Log.Entity, exc, $"There was an error loading entity at {dataPath}: $e");
        }
    }


    public EntityInstance Instantiate()
    {
        EntityInstance inst = Activator.CreateInstance(behaviourType, manager, this) as EntityInstance;
        manager.instances.Add(inst);
        return inst;
    }
}