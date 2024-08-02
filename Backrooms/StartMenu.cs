using System;
using System.Linq;
using System.Drawing;
using Backrooms.Gui;
using System.Collections;
using Backrooms.Debugging;
using Backrooms.Coroutines;
using Backrooms.SaveSystem;
using Backrooms.Entities;

namespace Backrooms;

public class StartMenu
{
    public readonly EntityManager entityManager;
    public readonly MpManager mpManager;
    public readonly Window win;
    public readonly FontFamily font;
    public readonly Camera cam;
    public readonly CameraController camController;
    public readonly Map map;
    public readonly Coroutine backgroundSequenceCoroutine;
    public readonly Game game;

    public readonly GuiGroup startGui, settingsGui, spGui, mpGui, mpHostGui, mpJoinGui;


    public StartMenu(Game game, Window win, Renderer rend, Camera cam, CameraController camController, Map map, MpManager mpManager, EntityManager entityManager, string fontFamily = "cascadia_code")
    {
        this.win = win;
        this.mpManager = mpManager;
        this.cam = cam;
        this.camController = camController;
        this.map = map;
        this.entityManager = entityManager;
        this.game = game;

        font = Resources.fonts[fontFamily];

        ColorBlock colors = new(Color.Black, 125, 185, 225);
        Color textColor = Color.Yellow;

        startGui = new(rend, "sm_start", false, true) {
            new TextElement("title", "The Backrooms", font, 30f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new ButtonElement("start_sp", "Singleplayer", font, 15f, textColor, colors, true, () => SwitchGui(startGui, spGui), new(.5f, .4f), new(.4f, .1f)),
            new ButtonElement("start_mp", "Multiplayer", font, 15f, textColor, colors, true, () => SwitchGui(startGui, mpGui), new(.5f, .525f), new(.4f, .1f)),
            new ButtonElement("settings", "Settings", font, 15f, textColor, colors, true, OpenSettings, new(.5f, .65f), new(.4f, .1f)),
            new ButtonElement("quit", "Quit", font, 15f, textColor, colors, true, () => Window.Exit(), new(.5f, .775f), new(.4f, .1f)),

            //new InputFieldElement("text_test", font, 12.5f, textColor, colors, new(.5f, .9f), new(.4f, .1f))
        };
        //startGui.GetElement<InputFieldElement>("text_test").valueChanged += v => Out(Log.Debug, v);

        spGui = new(rend, "sm_sp", false, false) {
            new TextElement("title", "Singleplayer", font, 25f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new TextElement("temp_message", "Imagine there was a list of\nloadable entities here pls", font, 15f, textColor, Vec2f.half, new(.5f, .4f), Vec2f.zero),

            new ButtonElement("start", "Start", font, 15f, textColor, colors, true, ClickStartSp, new(.5f, .7f), new(.2f, .1f)),

            new ButtonElement("back", "Back", font, 15f, textColor, colors, true, () => SwitchGui(spGui, startGui), new(.5f, .85f), new(.2f, .1f)),
        };

        mpGui = new(rend, "sm_mp", false, false) {
            new TextElement("title", "Multiplayer", font, 25f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new ButtonElement("host", "Host", font, 15f, textColor, colors, true, () => SwitchGui(mpGui, mpHostGui), new(.5f, .3f), new(.2f, .1f)),
            new ButtonElement("join", "Join", font, 15f, textColor, colors, true, () => SwitchGui(mpGui, mpJoinGui), new(.5f, .4f), new(.2f, .1f)),

            new ButtonElement("back", "Back", font, 15f, textColor, colors, true, () => SwitchGui(mpGui, startGui), new(.5f, .85f), new(.2f, .1f)),
        };

        mpHostGui = new(rend, "sm_mp_host", false, false) {
            new TextElement("title", "Multiplayer - Host", font, 25f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new ButtonElement("start", "Start", font, 15f, textColor, colors, true, () => ClickStartMp(true, 8080, "127.0.0.1"), new(.5f, .7f), new(.2f, .1f)),

            new ButtonElement("back", "Back", font, 15f, textColor, colors, true, () => SwitchGui(mpHostGui, mpGui), new(.5f, .85f), new(.2f, .1f)),
        };

        string[] allIpValues = Enumerable.Range(0, 256).Select(i => i.ToString()).ToArray();
        mpJoinGui = new(rend, "sm_mp_join", false, false) {
            new TextElement("title", "Multiplayer - Join", font, 25f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new ValueSelectorElement("field1", allIpValues, 127, textColor, font, 12.5f, colors, true, "up_arrow", .8f, null, new(.5f, .3f), new(.5f, .1f)),
            new ValueSelectorElement("field2", allIpValues, 0, textColor, font, 12.5f, colors, true, "up_arrow", .8f, null, new(.5f, .4f), new(.5f, .1f)),
            new ValueSelectorElement("field3", allIpValues, 0, textColor, font, 12.5f, colors, true, "up_arrow", .8f, null, new(.5f, .5f), new(.5f, .1f)),
            new ValueSelectorElement("field4", allIpValues, 1, textColor, font, 12.5f, colors, true, "up_arrow", .8f, null, new(.5f, .6f), new(.5f, .1f)),

            new ButtonElement("start", "Start", font, 15f, textColor, colors, true, () => {
                string ip = $"{mpJoinGui.GetElement<ValueSelectorElement>("field1").value}.{mpJoinGui.GetElement<ValueSelectorElement>("field2").value}.{mpJoinGui.GetElement<ValueSelectorElement>("field3").value}.{mpJoinGui.GetElement<ValueSelectorElement>("field4").value}";
                ClickStartMp(false, 8080, ip);
            }, new(.5f, .7f), new(.2f, .1f)),

            new ButtonElement("back", "Back", font, 15f, textColor, colors, true, () => SwitchGui(mpJoinGui, mpGui), new(.5f, .85f), new(.2f, .1f)),
        };

        Vec2i native = rend.physRes;
        Vec2i[] resolutions = [native/10, native/8, native/6, native/4, native/2, native];
        settingsGui = new(rend, "sm_settings", false, false) {
            new TextElement("title", "Settings", font, 25f, textColor, Vec2f.half, new(.5f, .2f), Vec2f.zero),

            new CheckboxElement("show_debug", "Show Debug Info", font, 15f, textColor, colors, true, "checkmark", .8f, true,
                b => {
                    rend.FindGuiGroup("debug").enabled = b;
                    SaveManager.settings.showDebugInfo = b;
                }, new(.5f, .4f), new(.65f, .065f)),
            new CheckboxElement("dev_console", "Dev Console", font, 15f, textColor, colors, true, "checkmark", .8f, false,
                b => {
                    DevConsole.ShowWindow(b ? DevConsole.WindowMode.Restore : DevConsole.WindowMode.Hide);
                    SaveManager.settings.devConsole = b;
                }, new(.5f, .5f), new(.65f, .065f)),
            new ValueSelectorElement("resolution", resolutions.Select(r => $"{r.x}x{r.y}").ToArray(), 2, textColor, font, 15f, colors, true, "up_arrow", .8f,
                i => {
                    rend.UpdateResolution(resolutions[i], rend.physRes);
                    SaveManager.settings.resolutionIndex = i;
                }, new(.5f, .6f), new(.65f, .065f), Vec2f.half),

            new ButtonElement("back", "Back", font, 15f, textColor, colors, true, CloseSettings, new(.5f, .85f), new(.2f, .1f)),
        };

        rend.guiGroups.Add(startGui);
        rend.guiGroups.Add(settingsGui);
        rend.guiGroups.Add(spGui);
        rend.guiGroups.Add(mpGui);
        rend.guiGroups.Add(mpJoinGui);
        rend.guiGroups.Add(mpHostGui);

        backgroundSequenceCoroutine = BackgroundSequence(.75f, .5f, 7).StartCoroutine(win);
    }


    private void ClickStartSp()
    {
        backgroundSequenceCoroutine.Cancel();
        win.SetCursor(false);
        camController.canMove = true;

        spGui.enabled = false;

        game.GenerateMap(0);
    }

    private void ClickStartMp(bool host, int port, string ip)
    {
        backgroundSequenceCoroutine.Cancel();
        win.SetCursor(false);
        camController.canMove = true;

        mpHostGui.enabled = false;
        mpJoinGui.enabled = false;

        mpManager.Start(host, host ? Utils.GetLocalIPAddress() : ip, port);
    }

    private void OpenSettings()
    {
        SaveManager.Load(SaveFile.Settings);

        settingsGui.GetElement<CheckboxElement>("show_debug").isOn = SaveManager.settings.showDebugInfo;
        settingsGui.GetElement<CheckboxElement>("dev_console").isOn = SaveManager.settings.devConsole;
        settingsGui.GetElement<ValueSelectorElement>("resolution").value = SaveManager.settings.resolutionIndex;

        startGui.enabled = false;
        settingsGui.enabled = true;
    }

    private void CloseSettings()
    {
        SaveManager.Save(SaveFile.Settings);

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


    private static void SwitchGui(GuiGroup disable, GuiGroup enable)
    {
        disable.enabled = false;
        enable.enabled = true;
    }
}