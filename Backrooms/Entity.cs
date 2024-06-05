using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Backrooms.Pathfinding;
using IO = System.IO;

namespace Backrooms;

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


    public Entity(Game game, string dataPath)
    {
        Stream memStream = null;

        try
        {
            using ZipArchive zip = File.GetAttributes(dataPath).HasFlag(FileAttributes.Directory) 
                ? new(memStream = Utils.ZipDirectoryInMemory(dataPath), ZipArchiveMode.Update)
                : ZipFile.Open(dataPath, ZipArchiveMode.Update);

            // Tags
            if(zip.GetEntry("tags.json") is var tagsEntry && tagsEntry is not null)
                using(Stream stream = tagsEntry.Open())
                    using(StreamReader reader = new(stream))
                        tags = JsonSerializer.Deserialize<EntityTags>(reader.ReadToEnd());

            // Sprite
            if(zip.GetEntry("sprite", [".png", ".jpg", ".jpeg"]) is var spriteEntry && spriteEntry is not null)
                using(Stream stream = spriteEntry.Open())
                    sprite = new(Image.FromStream(stream));
            else
                sprite = new(Resources.sprites["missing"]);

            // Audio
            if(zip.GetEntry("audio", [".mp3", ".wav", ".aiff"]) is var audioEntry && audioEntry is not null)
                audio = new(audioEntry.Open(), IO::Path.GetExtension(audioEntry.Name)) {
                    loop = true
                };

            // Behaviour
            List<string> sourceFiles = [];
            foreach(ZipArchiveEntry entry in from e in zip.Entries 
                                             where IO::Path.GetExtension(e.Name).Equals(".cs", StringComparison.CurrentCultureIgnoreCase)
                                             select e)
            {
                using Stream stream = entry.Open();
                using StreamReader reader = new(stream);

                sourceFiles.Add(reader.ReadToEnd());
            }

            behaviour = CsCompiler.BuildAssembly([..sourceFiles], IO::Path.GetFileNameWithoutExtension(dataPath).Replace(' ', '-').ToLower());
            behaviourType = behaviour.GetType(tags.instance);
            instance = Activator.CreateInstance(behaviourType, game, this);

            Assembly pathfindingAssembly = tags.builtinPathfinding ? Assembly.GetExecutingAssembly() : behaviour;
            pathfinding = new(game.map, Activator.CreateInstance(pathfindingAssembly.GetType(tags.pathfinding)) as IPathfindingAlgorithm);

            foreach(EntityTags.Function func in tags.functions)
            {
                MethodInfo method = behaviourType.GetMethod(func.name);
                functions[func.id] = method;

                switch(func.id)
                {
                    case "tick_dt": game.window.tick += dt => method.Invoke(instance, [dt]); break;
                    case "tick": game.window.tick += _ => method.Invoke(instance, null); break;
                    case "awake": game.mpHandler.onFinishConnect += () => method.Invoke(instance, null); break;
                }
            }
        }
        catch(Exception exc)
        {
            Out($"There was an error loading entity at {dataPath}: {exc}", ConsoleColor.Red);
        }
        finally
        {
            memStream?.Dispose();
        }
    }
}