using System;
using System.Collections.Generic;
using System.Drawing;
using Backrooms.Gui;
using Backrooms.Online;

namespace Backrooms;

public class StartMenu
{
    public MPHandler mpHandler;
    public event Action<MPHandler> mpHandlerInitialized;

    private readonly Window win;
    private readonly Renderer rend;
    private readonly FontFamily font;
    private readonly Game game;

    public readonly GuiGroup startScreen, settingsScreen;


    public StartMenu(Window win, Game game, string fontFamily = "cascadia_code")
    {
        this.win = win;
        this.game = game;
        font = Resources.fonts[fontFamily];
        rend = win.renderer;

        Color baseColor = Color.FromArgb(0);
        ColorBlock colors = new(Color.FromArgb(75, baseColor), Color.FromArgb(150, baseColor), Color.FromArgb(220, baseColor), 0f);

        startScreen = new(rend, "sm_start", true) {
            new TextElement("title", "The Backrooms", font, 30f, Color.Yellow, Anchor.C, new(.5f, .2f), Vec2f.zero),
            new ButtonElement("start_sp", "Singleplayer", font, 15f, Color.Yellow, colors, () => ClickStart(false), new(.5f, .4f), new(.4f, .1f)),
            new ButtonElement("start_mp", "Multiplayer", font, 15f, Color.Yellow, colors, () => ClickStart(true), new(.5f, .525f), new(.4f, .1f)),
            new ButtonElement("settings", "Settings", font, 15f, Color.Yellow, colors, OpenSettings, new(.5f, .65f), new(.4f, .1f)),
            new ButtonElement("quit", "Quit", font, 15f, Color.Yellow, colors, () => Environment.Exit(0), new(.5f, .775f), new(.4f, .1f))
        };

        settingsScreen = new(rend, "sm_settings", false) {
            new TextElement("title", "Settings", font, 25f, Color.Yellow, Anchor.C, new(.5f, .2f), Vec2f.zero),
            new TextElement("info", "No settings currently heheheha", font, 12f, Color.Yellow, Anchor.C, new(.5f, .4f), Vec2f.zero),
            new ButtonElement("back", "Back", font, 15f, Color.Yellow, colors, CloseSettings, new(.5f, .75f), new(.2f, .1f))
        };

        rend.guiGroups.Add(startScreen);
        rend.guiGroups.Add(settingsScreen);

        //game.GenerateMap(0);
        targetPos = game.camera.pos.Floor() + Vec2f.half;
        win.tick += AnimateBackground;
    }


    private void ClickStart(bool mp)
    {
        if(!mp)
        {
            win.tick -= AnimateBackground;

            startScreen.enabled = false;
            win.cursorVisible = false;

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

    private Dir facing = Dir.South;
    private readonly Random rand = new();
    private float lTime = 100f;
    private readonly float secsPerTile = 2f;
    private float tTime = 100f;
    private readonly float secsPerTurn = 2f;
    private bool turning = false;
    private Vec2f originPos, targetPos;
    private float originAngle, targetAngle;
    private void AnimateBackground(float dt)
    {
        if(turning)
        {
            if(tTime >= secsPerTurn)
            {
                game.camera.angle = targetAngle;
                tTime = 0f;
                turning = false;
            }
            else
            {
                tTime += dt;
                game.camera.angle = Utils.Lerp(originAngle, targetAngle, tTime/secsPerTurn);
            }
        }

        if(lTime >= secsPerTile)
        {
            game.camera.pos = targetPos;
            lTime = 0f;

            List<Dir> dirs = [Dir.North, Dir.South, Dir.East, Dir.West];
            dirs.Remove(facing);
            dirs.Shuffle(rand);
            
            Vec2i tile = game.camera.pos.Floor();
            for(int i = 0; i < dirs.Count; i++)
            {
                Vec2i newTile = tile + dirs[i].ToVec2f().Round();
                if(Map.IsEmptyTile(game.map[newTile]))
                {
                    targetPos = newTile + Vec2f.half;
                    originPos = tile + Vec2f.half;
                    turning = true;
                    originAngle = facing.ToAngle();
                    targetAngle = dirs[i].ToAngle();
                    facing = dirs[i];
                    return;
                }
            }

            originPos = targetPos;
            targetPos = originPos + facing.ToVec2f();

            Out(originPos);
            Out(targetPos);
        }
        else
        {
            lTime += dt;
            game.camera.pos = Vec2f.Lerp(originPos, targetPos, lTime/secsPerTile);
        }
    }
}