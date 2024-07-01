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

public class Entity
{
    public Vec2f pos;
    public readonly EntityTags tags;
    public readonly UnsafeGraphic sprite;
    public readonly AudioSource audio;
    public readonly Assembly behaviour;
    public readonly Type behaviourType;
    public readonly object instance;
    public readonly Dictionary<string, MethodInfo> functions = [];
    public readonly PathfindingEntity pathfinding;


    public Entity(EntityManager manager, string dataPath)
    {
        try
        {
            string path(string fileName)
                => $"{dataPath}/{fileName}";
            IEnumerable<string> fromPattern(string pattern)
                => Directory.GetFiles(dataPath, pattern, SearchOption.AllDirectories);
            IEnumerable<string> fromExtensions(string name, IEnumerable<string> extensions)
                => from f in fromPattern($"{name}.*")
                   from e in extensions
                   where f.EndsWith(e, StringComparison.OrdinalIgnoreCase)
                   select f;
            string text(string fileName)
                => File.ReadAllText(path(fileName));

            // Tags
            if(text("tags.json") is string tagsJson)
                tags = JsonSerializer.Deserialize<EntityTags>(tagsJson);

            // Sprite
            if(fromExtensions("sprite", [".png", ".jpg", ".jpeg"]).FirstOrDefault() is string spritePath and not null)
                using(Stream stream = new FileStream(spritePath, FileMode.Open))
                    sprite = new(Image.FromStream(stream));
            else
                sprite = new(Resources.sprites["missing"]);

            // Audio
            if(fromExtensions("audio", [".mp3", ".wav", ".aiff"]).FirstOrDefault() is string audioPath and not null)
            {
                audio = new(audioPath, true) {
                    volume = 0f
                };

                manager.entityActivate += audio.Play;
            }

            // Behaviour
            IEnumerable<string> sourceFiles = from f in fromExtensions("*", [".cs"])
                                              select File.ReadAllText(f);

            behaviour = CsCompiler.BuildAssembly([..sourceFiles], IO::Path.GetFileNameWithoutExtension(dataPath).Replace(' ', '-').ToLower());
            behaviourType = behaviour.GetType(tags.instance);
            instance = Activator.CreateInstance(behaviourType, manager.game, this);


            foreach(EntityTags.Function func in tags.functions)
            {
                MethodInfo method = behaviourType.GetMethod(func.name);
                functions[func.id] = method;

                switch(func.id)
                {
                    case "tick_dt": manager.entityTick += dt => method.Invoke(instance, [dt]); break;
                    case "tick": manager.entityTick += _ => method.Invoke(instance, null); break;
                    case "awake": manager.entityActivate += () => method.Invoke(instance, null); break;
                    case "get_volume": manager.entityTick += _ => audio.volume = Utils.Clamp01((float)method.Invoke(instance, [(manager.camera.pos - pos).length])); break;
                }
            }

            if(tags.managedPathfinding is EntityTags.ManagedPathfinding pathfindingData)
            {
                Assembly pathfindingAssembly = pathfindingData.builtinAlgorithm ? Assembly.GetExecutingAssembly() : behaviour;
                pathfinding = new(manager.map, Activator.CreateInstance(pathfindingAssembly.GetType(pathfindingData.algorithmName)) as IPathfindingAlgorithm);

                manager.entityTick += dt => pos = pathfinding.MoveTowards(pos, tags.size/2f, pathfindingData.speed, dt);
            }

            Out($"Successfully loaded entity at {dataPath}");
        }
        catch(Exception exc)
        {
            Out($"There was an error loading entity at {dataPath}: {exc}", ConsoleColor.Red);
        }
    }
}