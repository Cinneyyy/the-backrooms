using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Reflection;
using NAudio.CoreAudioApi;
using NAudio.Wave;

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
    public static readonly Dictionary<string, WaveStream> audios = [];
    public static readonly bool finishedInit;

    private static readonly StringDictionary manifests = [];
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();


    static Resources()
    {
        foreach(string manifest in assembly.GetManifestResourceNames())
        {
            string ext = Path.GetExtension(manifest).ToLower();
            ResType type = ext switch {
                ".png" or ".jpg" or ".jpeg" => ResType.Image,
                ".ttf" or ".otf" => ResType.Font,
                ".ico" => ResType.Icon,
                ".mp3" or ".wav" or ".aiff" => ResType.Audio,
                _ => ResType.Unknown
            };

            if(type == ResType.Unknown)
                continue;

            string name = manifest.Split('.')[^2];
            manifests.Add(name, manifest);

            Stream stream = assembly.GetManifestResourceStream(manifest);
            switch(type)
            {
                case ResType.Image: sprites.Add(name, Image.FromStream(stream)); break;
                case ResType.Icon: icons.Add(name, new(stream)); break;
                case ResType.Audio: audios.Add(name, ext switch {
                    ".mp3" => new Mp3FileReader(stream),
                    ".wav" => new WaveFileReader(stream),
                    ".aiff" => new AiffFileReader(stream),
                    _ => throw new()
                }); break;
                default: break;
            }

            if(type != ResType.Audio)
                stream.Dispose();
        }

        finishedInit = true;
    }


    public static Stream GetStream(string resourceName)
        => assembly.GetManifestResourceStream(manifests[resourceName]);
}