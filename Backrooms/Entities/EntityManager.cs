using System.IO.Compression;
using System.IO;
using System.Linq;
using System;

namespace Backrooms.Entities;

public class EntityManager(MpManager mpManager, Window window, Map map, Camera camera, Game game)
{
    public readonly MpManager mpHandler = mpManager;
    public readonly Window window = window;
    public readonly Map map = map;
    public readonly Game game = game;
    public readonly Camera camera = camera;
    public event Action entityActivate;
    public event Action<float> entityTick;


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
    }
}