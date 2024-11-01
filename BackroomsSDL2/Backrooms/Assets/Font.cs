using System;

namespace Backrooms.Assets;

public class Font(string path, int ptSize) : IDisposable
{
    ~Font()
        => Dispose();


    public readonly nint sdlFont = TTF_OpenFont(path, ptSize);


    private int _ptSize = ptSize;
    public int ptSize
    {
        get => _ptSize;
        set
        {
            _ptSize = value;
            if(TTF_SetFontSize(sdlFont, value) != 0)
                throw new($"Failed to change font size: {TTF_GetError()}");
        }
    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
        TTF_CloseFont(sdlFont);
    }


    public static Font FromName(string name, int ptSize)
        => Resources.LoadFont(name, ptSize);
}