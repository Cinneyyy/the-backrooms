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
    public Vec2f forward => Vec2f.FromAngle(angle);
    public Vec2f right => Vec2f.FromAngle(angle - MathF.PI/2f);


    public void RotateRad(float rad)
        => angle += rad;

    public void RotateDeg(float deg)
        => angle += deg * Utils.Rad2Deg;
}