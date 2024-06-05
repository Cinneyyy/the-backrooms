using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Backrooms;

public class Entity
{
    public readonly UnsafeGraphic sprite;
    public readonly AudioSource audio;
    public readonly Assembly behaviour;


    public Entity(Game game, string dataPath)
    {
        Stream memStream = null;

        try
        {
            using ZipArchive zip = File.GetAttributes(dataPath).HasFlag(FileAttributes.Directory) 
                ? new(memStream = Utils.ZipDirectoryInMemory(dataPath), ZipArchiveMode.Read)
                : ZipFile.OpenRead(dataPath);

            // Sprite
            if(zip.GetEntry("sprite", [".png", ".jpg", ".jpeg"]) is var spriteEntry && spriteEntry is not null)
                using(Stream stream = spriteEntry.Open())
                    sprite = new(Image.FromStream(stream));
            else
                sprite = new(Resources.sprites["missing"]);

            // Audio
            //if(zip.GetEntry("audio", [".mp3", ".wav", ".aiff"]) is var audioEntry && audioEntry is not null)
            //        audio = new(audioEntry.Open(), Path.GetExtension(audioEntry.Name));

            // Behaviour
            List<string> sourceFiles = [];
            foreach(ZipArchiveEntry entry in from e in zip.Entries 
                                             where Path.GetExtension(e.Name).Equals(".cs", StringComparison.CurrentCultureIgnoreCase)
                                             select e)
            {
                using Stream stream = entry.Open();
                using StreamReader reader = new(stream);

                sourceFiles.Add(reader.ReadToEnd());
            }

            behaviour = CsCompiler.BuildAssembly([..sourceFiles], "entity-behaviour");
            Out(sourceFiles.Count);
            Out(sourceFiles.FormatStr("\n"));
            Out(behaviour.GetTypes().FormatStr(", ", t => t.FullName));

            object behaviourInst = Activator.CreateInstance(behaviour.GetType("Behaviour"), game);
            MethodInfo meth = behaviourInst.GetType().GetMethod("Tick");
            game.window.tick += dt => meth.Invoke(behaviourInst, [dt]);
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