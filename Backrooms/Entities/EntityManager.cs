using System.IO.Compression;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Backrooms.Entities;

public class EntityManager(MpManager mpManager, Window window, Map map, Camera camera, Game game, Renderer rend)
{
    public readonly MpManager mpManager = mpManager;
    public readonly Window window = window;
    public readonly Map map = map;
    public readonly Game game = game;
    public readonly Camera camera = camera;
    public readonly Renderer rend = rend;
    public readonly List<EntityType> types = [];
    public readonly List<EntityInstance> instances = [];


    public void LoadEntities(string directoryPath)
    {
        foreach(string path in
            Directory.GetDirectories(directoryPath, "", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(directoryPath, "*.zip", SearchOption.TopDirectoryOnly)))
            LoadEntity(path);
    }

    public void LoadEntity(string directoryPath)
    {
        if(Path.GetExtension(directoryPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            string zipPath = directoryPath;
            directoryPath = Path.GetFileNameWithoutExtension(directoryPath);
            ZipFile.ExtractToDirectory(zipPath, directoryPath);
        }

        EntityType data = new(this, directoryPath);
        types.Add(data);
    }

    public EntityInstance Instantiate(string typeName)
        => types.Find(e => e.tags.instance == typeName).Instantiate();
}