using Backrooms.Assets;

namespace Backrooms;

public class Text
{
    public Text(string text, SDL_Color color, Font font, Vec2i loc = default)
    {
        _text = text;
        _font = font;
        _color = color;
        this.loc = loc;

        Update();
    }

    public Text(string text, SDL_Color color, string font, int ptSize, Vec2i loc = default)
        : this(text, color, Resources.LoadFont(font, ptSize), loc) { }


    public Vec2i loc;

    private nint cached;


    private string _text;
    public string text
    {
        get => _text;
        set
        {
            _text = value;
            Update();
        }
    }

    private Font _font;
    public Font font
    {
        get => _font;
        set
        {
            _font = value;
            Update();
        }
    }

    private SDL_Color _color;
    public SDL_Color color
    {
        get => _color;
        set
        {
            _color = value;
            Update();
        }
    }


    public void Render()
        => Renderer.RenderTex(cached, loc);


    private void Update()
        => cached = Renderer.DrawText(_text, _font, _color);
}