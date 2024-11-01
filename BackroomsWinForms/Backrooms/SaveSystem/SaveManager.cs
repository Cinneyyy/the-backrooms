using System;
using System.IO;
using System.IO.Compression;
using Backrooms.Serialization;

namespace Backrooms.SaveSystem;

public static class SaveManager
{
    public static readonly SettingsFile settings = new();


    static SaveManager()
    {
        if(!Directory.Exists("saves"))
            Directory.CreateDirectory("saves");
    }


    public static void Load(SaveFile file)
    {
        if(file.HasFlag(SaveFile.Settings)) ReadAndLoad("saves/settings.bin", settings);
    }

    public static void Save(SaveFile file)
    {
        if(file.HasFlag(SaveFile.Settings)) SaveAndWrite("saves/settings.bin", settings);
    }


    private static void ReadAndLoad<T>(string fileLocation, T target) where T : Serializable<T>, new()
    {
        try
        {
            byte[] data = File.ReadAllBytes(fileLocation);
            BinarySerializer<T>.DeserializeRef(data, ref target, false);
        }
        catch(Exception exc)
        {
            OutErr(Log.Info, exc, $"There was an error when attempting to load save file at {fileLocation} (type {typeof(T).Name}), though this may just mean that no file currently exists");
        }
    }

    private static void SaveAndWrite<T>(string fileLocation, T target) where T : Serializable<T>, new()
    {
        try
        {
            byte[] data = BinarySerializer<T>.Serialize(target, CompressionLevel.NoCompression);
            File.WriteAllBytes(fileLocation, data);
        }
        catch(Exception exc)
        {
            OutErr(Log.Info, exc, $"There was an error when attempting to save file to {fileLocation} (type {typeof(T).Name})");
        }
    }
}