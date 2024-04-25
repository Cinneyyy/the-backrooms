using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;

namespace Backrooms;

public static class Resources
{
    public enum ResType
    {
        Image,
        Font,
        Icon,
        Audio,
        Unknown
    }


    public static readonly Dictionary<string, Image> sprites = [];
    public static readonly Dictionary<string, Icon> icons = [];
    public static readonly Dictionary<string, SoundPlayer> audios = [];

    private static readonly StringDictionary manifests = [];
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();


    static Resources()
    {
        foreach(string manifest in assembly.GetManifestResourceNames())
        {
            ResType type = Path.GetExtension(manifest) switch {
                ".png" or ".jpg" or ".jpeg" => ResType.Image,
                ".ttf" or ".otf" => ResType.Font,
                ".ico" => ResType.Icon,
                ".mp3" or ".wav" or ".ogg" => ResType.Audio,
                _ => ResType.Unknown
            };

            if(type == ResType.Unknown)
                continue;

            string name = manifest.Split('.')[^2];
            manifests.Add(name, manifest);

            using Stream stream = assembly.GetManifestResourceStream(manifest);
            switch(type)
            {
                case ResType.Image: sprites.Add(name, Image.FromStream(stream)); break;
                case ResType.Icon: icons.Add(name, new(stream)); break;
                case ResType.Audio: 
                    audios.Add(name, new(stream)); 
                    audios[name].Load();
                    break;
                default: break;
            }
        }
    }


    public static Stream GetStream(string resourceName)
        => assembly.GetManifestResourceStream(manifests[resourceName]);
}