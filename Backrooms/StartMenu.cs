﻿using System;
using System.Linq;
using System.Drawing;
using Backrooms.Gui;
using Backrooms.Online;
using System.Collections;
using Backrooms.Coroutines;

namespace Backrooms;

public class StartMenu
{
    public MPHandler mpHandler;
    public event Action<MPHandler> mpHandlerInitialized;

    private readonly Window win;
    private readonly Renderer rend;
    private readonly FontFamily font;
    private readonly Game game;
    private Coroutine backgroundSequenceCoroutine;

    public readonly GuiGroup startScreen, settingsScreen;


    public StartMenu(Window win, Game game, string fontFamily = "cascadia_code")
    {
        this.win = win;
        this.game = game;
        font = Resources.fonts[fontFamily];
        rend = win.renderer;

        Color baseColor = Color.FromArgb(0);
        ColorBlock colors = new(Color.FromArgb(125, baseColor), Color.FromArgb(185, baseColor), Color.FromArgb(225, baseColor), 0f);

        startScreen = new(rend, "sm_start", false, true) {
            new TextElement("title", "The Backrooms", font, 30f, Color.Yellow, Anchor.C, new(.5f, .2f), Vec2f.zero),
            
            new ButtonElement("start_sp", "Singleplayer", font, 15f, Color.Yellow, colors, () => ClickStart(false), new(.5f, .4f), new(.4f, .1f)),
            new ButtonElement("start_mp", "Multiplayer", font, 15f, Color.Yellow, colors, () => ClickStart(true), new(.5f, .525f), new(.4f, .1f)),
            new ButtonElement("settings", "Settings", font, 15f, Color.Yellow, colors, OpenSettings, new(.5f, .65f), new(.4f, .1f)),
            new ButtonElement("quit", "Quit", font, 15f, Color.Yellow, colors, () => Window.Exit(), new(.5f, .775f), new(.4f, .1f)),
        };

        Vec2i hd = new(1920, 1080);
        Vec2i[] resolutions = [hd/10, hd/8, hd/6, hd/4, hd/2, hd];
        settingsScreen = new(rend, "sm_settings", false, false) {
            new TextElement("title", "Settings", font, 25f, Color.Yellow, Anchor.C, new(.5f, .2f), Vec2f.zero),

            new CheckboxElement("show_fps", "Show FPS", font, 15f, Color.Yellow, colors, "checkmark", .8f, true, b => rend.FindGuiGroup("hud").FindElement("fps").enabled = b, new(.5f, .4f), new(.65f, .065f)),
            new CheckboxElement("fix_fisheye", "Fix Fisheye", font, 15f, Color.Yellow, colors, "checkmark", .8f, true, b => rend.camera.fixFisheyeEffect = b, new(.5f, .5f), new(.65f, .065f)),
            new CheckboxElement("dev_console", "Dev Console", font, 15f, Color.Yellow, colors, "checkmark", .8f, false, b => DevConsole.ShowWindow(b ? DevConsole.WindowMode.Restore : DevConsole.WindowMode.Hide), new(.5f, .6f), new(.65f, .065f)),
            new ValueSelectorElement("resolution", (from r in resolutions select $"{r.x}x{r.y}").ToArray(), 0, Color.Yellow, font, 15f, colors, "up_arrow", .8f, i => rend.UpdateResolution(resolutions[i], rend.physRes), new(.5f, .7f), new(.65f, .065f), Anchor.C),
            
            new ButtonElement("back", "Back", font, 15f, Color.Yellow, colors, CloseSettings, new(.5f, .85f), new(.2f, .1f)),
        };

        rend.guiGroups.Add(startScreen);
        rend.guiGroups.Add(settingsScreen);

        IEnumerator background_sequence()
        {
            Camera cam = rend.camera;
            Map map = rend.map;

            game.GenerateMap(0);
            cam.pos = cam.pos.Floor() + Vec2f.half;

            while(true)
            {
                Vec2i offset = (Utils.Rad2Deg * cam.angle) switch {
                    (>= 0f and < 45f) or (< 360f and >= 315f) => Vec2i.right,
                    >= 45f and < 135f => Vec2i.up,
                    >= 135f and < 225f => Vec2i.left,
                    >= 225f and < 315f => Vec2i.down,
                    _ => Vec2i.zero
                };
                Vec2i tile = cam.pos.Floor();
                Vec2i offsetTile = tile + offset;

                bool isBlocked = Map.IsCollidingTile(map[offsetTile]);

                if(isBlocked)
                {
                    float start = cam.angle, end = cam.angle + MathF.PI/2f;
                    yield return new ActionOverTime(1.5f, interpolatedAction: t => cam.angle = Utils.Lerp(start, end, t));
                }
                else
                {
                    Vec2f start = cam.pos, end = cam.pos + offset;
                    yield return new ActionOverTime(1.5f, interpolatedAction: t => cam.pos = Vec2f.Lerp(start, end, t));
                }
            }
        }
        backgroundSequenceCoroutine = win.StartCoroutine(background_sequence());
    }


    private void ClickStart(bool mp)
    {
        backgroundSequenceCoroutine.Cancel();
        if(!mp)
        {
            startScreen.enabled = false;
            win.cursorVisible = false;
            win.input.lockCursor = true;

            mpHandler = new(game, true, "127.0.0.1", 8080);
            mpHandler.Start();

            mpHandlerInitialized?.Invoke(mpHandler);
        }
        else
        {
            // TODO: mp screen
        }
    }

    private void OpenSettings()
    {
        startScreen.enabled = false;
        settingsScreen.enabled = true;
    }

    private void CloseSettings()
    {
        startScreen.enabled = true;
        settingsScreen.enabled = false;
    }
}