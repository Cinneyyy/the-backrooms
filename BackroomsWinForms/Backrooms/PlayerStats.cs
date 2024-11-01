using System;

namespace Backrooms;

public class PlayerStats(Game game)
{
    public readonly Game game = game;
    public event Action takeDamage;

    private float _health = 1f, _saturation = 1f, _hydration = 1f, _sanity = 1f;


    public float health
    {
        get => _health;
        set {
            if(_health > value)
                takeDamage?.Invoke();

            _health = Utils.Clamp01(value);

            if(_health.RoughlyZero(1e-4f))
                game.KillPlayer();
        }
    }
    public float saturation
    {
        get => _saturation;
        set => _saturation = Utils.Clamp01(value);
    }
    public float hydration
    {
        get => _hydration;
        set => _hydration = Utils.Clamp01(value);
    }
    public float sanity
    {
        get => _sanity;
        set => _sanity = Utils.Clamp01(value);
    }
}