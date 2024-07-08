using System.IO.Compression;
using System.IO;
using System.Linq;
using System;

namespace Backrooms.Entities;

public class EntityManager(MpManager mpManager, Window window, Map map, Camera camera, Game game, Renderer rend)
{
    public readonly MpManager mpHandler = mpManager;
    public readonly Window window = window;
    public readonly Map map = map;
    public readonly Game game = game;
    public readonly Camera camera = camera;
    public readonly Renderer rend = rend;
    public event Action entityActivate, entityPulse;
    public event Action<float> entityTick, fixedEntityTick;


    public Entity[] entities { get; private set; }


    public void LoadEntities(string directoryPath)
    {
        foreach(string zip in Directory.GetFiles(directoryPath, "*.zip", SearchOption.TopDirectoryOnly))
            ZipFile.ExtractToDirectory(zip, Path.GetFileNameWithoutExtension(zip));

        entities = (from d in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly)
                    select new Entity(this, d))
                    .ToArray();

        mpHandler.connectedToServer += entityActivate;
        window.tick += entityTick;
        window.fixedTick += fixedEntityTick;
        window.pulse += entityPulse;
    }
}