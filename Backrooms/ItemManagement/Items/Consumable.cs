using System.Text;

namespace Backrooms.ItemManagement.Items;

public class Consumable(string name, string itemDesc, float healthVal, float saturationVal, float hydrationVal, float sanityVal, UnsafeGraphic graphic)
    : Item(name, Consumable.GenerateDescription(itemDesc, saturationVal, hydrationVal, healthVal, sanityVal), true, graphic)
{
    public readonly float
        healthVal = healthVal,
        saturationVal = saturationVal,
        hydrationVal = hydrationVal,
        sanityVal = sanityVal;


    protected override void OnUse(Game game)
    {
        PlayerStats stats = game.playerStats;

        stats.health += healthVal;
        stats.saturation += saturationVal;
        stats.hydration += hydrationVal;
        stats.sanity += sanityVal;
    }


    private static string GenerateDescription(string baseDesc, float healthVal, float hungerVal, float thirstVal, float sanityVal)
    {
        StringBuilder sb = new(baseDesc);

        if(healthVal > 0f || hungerVal > 0f || thirstVal > 0f || sanityVal > 0f)
            sb.Append('\n');

        if(healthVal > 0f) sb.Append($"\n- Restores {healthVal*100f:0%} health");
        if(hungerVal > 0f) sb.Append($"\n- Satisfies {hungerVal*100f:0%} hunger");
        if(thirstVal > 0f) sb.Append($"\n- Quenches {thirstVal*100f:0%} thirst");
        if(sanityVal > 0f) sb.Append($"\n- Stabilises {sanityVal*100f:0%} sanity");

        if(healthVal < 0f || hungerVal < 0f || thirstVal < 0f || sanityVal < 0f)
            sb.Append('\n');

        if(healthVal < 0f) sb.Append($"\n- Reduces {healthVal*100f:0%} health");
        if(hungerVal < 0f) sb.Append($"\n- Increases {hungerVal*100f:0%} hunger");
        if(thirstVal < 0f) sb.Append($"\n- Parches {thirstVal*100f:0%} thirst");
        if(sanityVal < 0f) sb.Append($"\n- Deteriorates {sanityVal*100f:0%} sanity");

        return sb.ToString();
    }
}