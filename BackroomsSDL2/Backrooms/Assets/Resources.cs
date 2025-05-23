using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;

namespace Backrooms.Assets;

public static class Resources
{
    static Resources()
    {
        Dictionary<string, string> fontPaths = [];
        Dictionary<string, string> texturePaths = [];
        Dictionary<string, Texture> textures = [];
        Dictionary<string, LockedTexture> lockedTextures = [];

        foreach(string path in Directory.GetFiles("res", "*.*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(path);
            string name = Path.GetFileNameWithoutExtension(path);

            switch(ext)
            {
                case ".ttf":
                {
                    fontPaths[name] = path;
                    break;
                }
                case ".png" or ".jpg":
                {
                    texturePaths[name] = path;
                    textures[name] = new(path);
                    lockedTextures[name] = new(path);
                    break;
                }
            }
        }

        Resources.fontPaths = fontPaths.ToFrozenDictionary();
        Resources.texturePaths = texturePaths.ToFrozenDictionary();
        Resources.textures = textures.ToFrozenDictionary();
        Resources.lockedTextures = lockedTextures.ToFrozenDictionary();
    }


    public static readonly FrozenDictionary<string, string> fontPaths;
    public static readonly FrozenDictionary<string, string> texturePaths;
    public static readonly FrozenDictionary<string, Texture> textures;
    public static readonly FrozenDictionary<string, LockedTexture> lockedTextures;


    public static Texture GetTexture(string name)
        => textures[name];
    public static LockedTexture GetLockedTexture(string name)
        => lockedTextures[name];

    public static Font LoadFont(string name, int ptSize)
        => new(fontPaths[name], ptSize);
    public static Texture LoadTexture(string name)
        => new(texturePaths[name]);
    public static LockedTexture LoadLockedTexture(string name)
        => new(texturePaths[name]);

    public static Font LoadFontFromPath(string path, int ptSize)
        => new(path, ptSize);
    public static Texture LoadTextureFromPath(string path)
        => new(path);
    public static LockedTexture LoadLockedTextureFromPath(string path)
        => new(path);
}