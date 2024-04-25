using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Backrooms;

public static class Resources
{
    public enum ResType
    {
        Image,
        Font,
        Icon,
        Unknown
    }


    public static readonly Dictionary<string, Image> sprites = [];
    public static readonly Dictionary<string, Icon> icons = [];


    static Resources()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach(string manifest in assembly.GetManifestResourceNames())
        {
            ResType type = Path.GetExtension(manifest) switch {
                ".png" or ".jpg" or ".jpeg" => ResType.Image,
                ".ttf" or ".otf" => ResType.Font,
                ".ico" => ResType.Icon,
                _ => ResType.Unknown
            };

            if(type == ResType.Unknown)
                continue;

            string name = manifest.Split('.')[^2];
            Stream stream = assembly.GetManifestResourceStream(manifest);
            switch(type)
            {
                case ResType.Image: sprites.Add(name, Image.FromStream(stream)); break;
                case ResType.Icon: icons.Add(name, new(stream)); break;
                default: break;
            }
        }
    }
}