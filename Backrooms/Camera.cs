using System;

namespace Backrooms;

public class Camera(float fovRadians, float maxDist)
{
    public float fov = fovRadians;
    public Vec2f pos;
    public float maxDist = maxDist;
    public bool fixFisheyeEffect = true;

    private float _angle;


    public float angle
    {
        get => _angle;
        set => _angle = Utils.NormAngle(value);
    }
    public float angleDeg
    {
        get => angle * Utils.Rad2Deg;
        set => angle = value * Utils.Deg2Rad;
    }
    public Vec2f forward => Vec2f.FromAngle(angle);
    public Vec2f right => Vec2f.FromAngle(angle - MathF.PI/2f);
    public Vec2f plane
    {
        get {
            Vec2f dir = forward * fovFactor;
            return new(dir.y, -dir.x);
        }
    }
    public float fovFactor => MathF.Tan(fov/2f);


    public void RotateRad(float rad)
        => angle += rad;

    public void RotateDeg(float deg)
        => angleDeg += deg;
}