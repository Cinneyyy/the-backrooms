using System;
using System.Linq;
using System.Drawing;
using Backrooms.Gui;
using System.Collections;
using Backrooms.Debugging;
using Backrooms.Coroutines;

namespace Backrooms;

public class StartMenu
{
    public MpManager mpManager;

    private readonly Window win;
    private readonly FontFamily font;
    private readonly Camera cam;
    private readonly CameraController camController;
    private readonly Map map;
    private readonly Coroutine backgroundSequenceCoroutine;

    public readonly GuiGroup startGui, settingsGui;


    public StartMenu(Window win, Renderer rend, Camera cam, CameraController camController, Map map, MpManager mpManager, string fontFamily = "cascadia_code")
    {
        this.win = win;
        this.mpManager = mpManager;
        this.cam = cam;
        this.camController = camController;
        this.map = map;

        font = Resources.fonts[fontFamily];

        ColorBlock colors = new(Color.Black, 125, 185, 225);

        startGui = new(rend, "sm_start", false, true) {
            new TextElement("title", "The Backrooms", font, 30f, Color.Yellow, Vec2f.half, new(.5f, .2f), Vec2f.zero),
            
            new ButtonElement("start_sp", "Singleplayer", font, 15f, Color.Yellow, colors, true, () => ClickStart(false), new(.5f, .4f), new(.4f, .1f)),
            new ButtonElement("start_mp", "Multiplayer", font, 15f, Color.Yellow, colors, true, () => ClickStart(true), new(.5f, .525f), new(.4f, .1f)),
            new ButtonElement("settings", "Settings", font, 15f, Color.Yellow, colors, true, OpenSettings, new(.5f, .65f), new(.4f, .1f)),
            new ButtonElement("quit", "Quit", font, 15f, Color.Yellow, colors, true, () => Window.Exit(), new(.5f, .775f), new(.4f, .1f)),
        };

        Vec2i native = rend.physRes;
        Vec2i[] resolutions = [native/10, native/8, native/6, native/4, native/2, native];
        settingsGui = new(rend, "sm_settings", false, false) {
            new TextElement("title", "Settings", font, 25f, Color.Yellow, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new CheckboxElement("show_debug", "Show Debug Info", font, 15f, Color.Yellow, colors, true, "checkmark", .8f, true, b => rend.FindGuiGroup("debug").enabled = b, new(.5f, .4f), new(.65f, .065f)),
            new CheckboxElement("dev_console", "Dev Console", font, 15f, Color.Yellow, colors, true, "checkmark", .8f, false, b => DevConsole.ShowWindow(b ? DevConsole.WindowMode.Restore : DevConsole.WindowMode.Hide), new(.5f, .5f), new(.65f, .065f)),
            new ValueSelectorElement("resolution", (from r in resolutions select $"{r.x}x{r.y}").ToArray(), 2, Color.Yellow, font, 15f, colors, true, "up_arrow", .8f, i => rend.UpdateResolution(resolutions[i], rend.physRes), new(.5f, .6f), new(.65f, .065f), Vec2f.half),
            
            new ButtonElement("back", "Back", font, 15f, Color.Yellow, colors, true, CloseSettings, new(.5f, .85f), new(.2f, .1f)),
        };

        rend.guiGroups.Add(startGui);
        rend.guiGroups.Add(settingsGui);
        
        backgroundSequenceCoroutine = BackgroundSequence(.75f, .5f, 7).StartCoroutine(win);
    }


    private void ClickStart(bool mp)
    {

        if(!mp)
        {
            // TODO: sp screen

            backgroundSequenceCoroutine.Cancel();
            startGui.enabled = false;
            win.SetCursor(false);
            camController.canMove = true;

            mpManager.Start(true, "127.0.0.1", 8080);

        }
        else
        {
            // TODO: mp screen

            backgroundSequenceCoroutine.Cancel();
            startGui.enabled = false;
            win.SetCursor(false);
            camController.canMove = true;

            mpManager.Start(false, "127.0.0.1", 8080);
        }
    }

    private void OpenSettings()
    {
        startGui.enabled = false;
        settingsGui.enabled = true;
    }

    private void CloseSettings()
    {
        startGui.enabled = true;
        settingsGui.enabled = false;
    }

    private IEnumerator BackgroundSequence(float turnTime, float travelTime, int minTilesBeforeRandomTurn)
    {
        cam.pos = cam.pos.Floor() + Vec2f.half;

        static Vec2i get_offset_vec(float angle) // based on unit circle
            => (Utils.Rad2Deg * angle) switch {
                (>= 0f and < 45f) or (< 360f and >= 315f) => Vec2i.right,
                >= 45f and < 135f => Vec2i.up,
                >= 135f and < 225f => Vec2i.left,
                >= 225f and < 315f => Vec2i.down,
                _ => Vec2i.zero
            };

        int turnCooldown = 5;

        while(true)
        {
            Vec2i offset = get_offset_vec(cam.angle);
            Vec2i tile = cam.pos.Floor();

            bool isBlocked = Map.IsCollidingTile(map[tile + offset]);

            if(!isBlocked && turnCooldown <= 0)
            {
                bool leftBlocked = Map.IsCollidingTile(map[tile + get_offset_vec(Utils.NormAngle(cam.angle - MathF.PI/2f))]),
                     rightBlocked = Map.IsCollidingTile(map[tile + get_offset_vec(Utils.NormAngle(cam.angle + MathF.PI/2f))]);

                if(leftBlocked != rightBlocked)
                    isBlocked = RNG.coinToss;
            }

            if(isBlocked)
            {
                float turn = MathF.PI/2f * (RNG.coinToss ? -1f : 1f);
                if(Map.IsCollidingTile(map[tile + get_offset_vec(Utils.NormAngle(cam.angle + turn))]))
                    turn *= -1f;

                float start = cam.angle, end = cam.angle + turn;
                yield return new ActionOverTime(turnTime, interpolatedAction: t => cam.angle = Utils.Lerp(start, end, t));

                turnCooldown = minTilesBeforeRandomTurn;
            }
            else
            {
                Vec2f start = cam.pos, end = cam.pos + offset;
                yield return new ActionOverTime(travelTime, interpolatedAction: t => cam.pos = Vec2f.Lerp(start, end, t));

                turnCooldown--;
            }
        }
    }
}