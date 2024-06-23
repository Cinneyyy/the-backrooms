namespace Backrooms;

public class PlayerStats(float maxHealth, float maxHunger, float maxThirst, float maxSanity)
{
    public readonly float maxHealth = maxHealth, maxHunger = maxHunger, maxThirst = maxThirst, maxSanity = maxSanity;
    public float health = maxHealth, hunger = maxHunger, thirst = maxThirst, sanity = maxSanity;
}