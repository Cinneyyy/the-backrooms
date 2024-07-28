using System.Collections;
using System.Drawing;
using Backrooms.Coroutines;
using Backrooms.Gui;

namespace Backrooms;

public partial class DeathScreen
{
    public readonly Renderer rend;
    public readonly Window win;
    public readonly Game game;
    public readonly PlayerStats playerStats;
    public readonly CameraController camController;

    private readonly GuiGroup gui;
    private readonly ButtonElement continueButton, quitButton;


    public DeathScreen(CameraController camController, PlayerStats playerStats, Game game, Renderer rend, Window win, StartMenu startMenu, string fontFamily = "cascadia_code")
    {
        this.camController = camController;
        this.playerStats = playerStats;
        this.game = game;
        this.rend = rend;
        this.win = win;

        FontFamily font = Resources.fonts[fontFamily];
        ColorBlock colors = new(Color.Black, 125, 185, 225);

        gui = new(rend, "death_screen", true, false) {
            new ImageElement("background", graphic: null, Color.White, Vec2f.zero, new Vec2f(rend.virtRatio, 1f), Vec2f.zero),
            new TextElement("title", "You dead brother", font, 30f, Color.Red, Vec2f.half, new(.5f, .25f), Vec2f.zero) { enabled = false },

            (continueButton = new ButtonElement("continue", "Continue", font, 15f, Color.Yellow, colors, true, Disable, new(.5f, .4f), new(.4f, .1f)) { enabled = false }),
            (quitButton = new ButtonElement("quit", "Quit", font, 15f, Color.Yellow, colors, true, () => {
                Disable();
                win.SetCursor(true);
                startMenu.startGui.enabled = true;
            }, new(.5f, .525f), new(.4f, .1f)) { enabled = false }),
        };

        rend.guiGroups.Add(gui);
    }


    public void Enable()
    {
        gui.enabled = true;
        Bitmap lastView = rend.Draw();
        gui.GetElement<ImageElement>("background").graphic = new(lastView);
        PlayAnimation().StartCoroutine(win);
    }

    public void Disable()
    {
        gui.enabled = false;
        gui.GetElement<ImageElement>("background").mul = 1f;
        gui.GetElement("title").enabled = false;
        continueButton.enabled = false;
        quitButton.enabled = false;
        game.playerDead = false;
        playerStats.health = 1f;
        camController.canMove = true;
        win.SetCursor(false);
    }


    private IEnumerator PlayAnimation()
    {
        Coroutine.DelayedAction(2.5f, () => gui.GetElement("title").enabled = true).StartCoroutine(win);

        ImageElement bgElem = gui.GetElement<ImageElement>("background");
        yield return new ActionOverTime(5f, interpolatedAction: t => bgElem.mul = 1f - t);

        continueButton.enabled = true;
        quitButton.enabled = true;
    }
}