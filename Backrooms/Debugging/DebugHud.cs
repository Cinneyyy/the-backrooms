using System.Drawing;
using Backrooms.Gui;

namespace Backrooms.Debugging;

public class DebugHud
{
    public readonly Game game;

    private readonly GuiGroup guiGroup;
    private readonly TextElement textLhs, textRhs;


    public bool enabled
    {
        get => guiGroup.enabled;
        set => guiGroup.enabled = value;
    }


    public DebugHud(Game game, bool enabled, FontFamily font, float emSize, Color color)
    {
        this.game = game;
        guiGroup = new(game.rend, "debug", true, enabled) {
            (textLhs = new("lhs", "", font, emSize, color, Vec2f.zero, Vec2f.zero, Vec2f.zero)),
            (textRhs = new("rhs", "", font, emSize, color, Vec2f.right, Vec2f.right, Vec2f.zero))
        };
        game.rend.guiGroups.Add(guiGroup);
        game.win.tick += Tick;
    }


    private void Tick(float dt)
    {
        if(!guiGroup.enabled)
            return;

        textLhs.text =
        $"""
        {game.win.currFps} fps
        {(game.mpManager.isConnected ? $"Client #{game.mpManager.clientId}" : "Not connected")}
        Pos: {game.camera.pos.Floor():$x, $y}
        Map size: {game.map.size}
        Seed: {game.generator.seed}
        Entities: {game.entityManager.instances.Count}
        Sprites: {game.rend.sprites.Count}
        """;

        textRhs.text =
        $"""
        Health: {game.playerStats.health:0%}
        Saturation: {game.playerStats.saturation:0%}
        Hydration: {game.playerStats.hydration:0%}
        Sanity: {game.playerStats.sanity:0%}
        """;
    }
}