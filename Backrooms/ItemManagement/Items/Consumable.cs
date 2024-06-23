using System.Text;

namespace Backrooms.ItemManagement.Items;

public class Consumable(string name, string itemDesc, float healthVal, float hungerVal, float thirstVal, float sanityVal, UnsafeGraphic graphic) : Item(name, Consumable.GenerateDescription(itemDesc, hungerVal, thirstVal, healthVal, sanityVal), true, graphic)
{
    public readonly float healthVal = healthVal, hungerVal = hungerVal, thirstVal = thirstVal, sanityVal = sanityVal;


    protected override void OnUse(Game game)
    {
        PlayerStats stats = game.playerStats;

        stats.health += healthVal;
        stats.hunger += hungerVal;
        stats.thirst += thirstVal;
        stats.sanity += sanityVal;
    }


    private static string GenerateDescription(string baseDesc, float healthVal, float hungerVal, float thirstVal, float sanityVal)
    {
        StringBuilder sb = new(baseDesc);

        if(healthVal > 0f || hungerVal > 0f || thirstVal > 0f || sanityVal > 0f)
            sb.Append('\n');

        if(healthVal > 0f) sb.Append($"\n- Restores {healthVal} health");
        if(hungerVal > 0f) sb.Append($"\n- Satisfies {hungerVal} hunger");
        if(thirstVal > 0f) sb.Append($"\n- Quenches {thirstVal} thirst");
        if(sanityVal > 0f) sb.Append($"\n- Stabilises {sanityVal} sanity");

        if(healthVal < 0f || hungerVal < 0f || thirstVal < 0f || sanityVal < 0f)
            sb.Append('\n');

        if(healthVal < 0f) sb.Append($"\n- Reduces {healthVal} health");
        if(hungerVal < 0f) sb.Append($"\n- Increases {hungerVal} hunger");
        if(thirstVal < 0f) sb.Append($"\n- Parches {thirstVal} thirst");
        if(sanityVal < 0f) sb.Append($"\n- Deteriorates {sanityVal} sanity");

        return sb.ToString();
    }
}