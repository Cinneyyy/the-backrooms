using System;

namespace Backrooms;

public class Camera(float fovRadians, float maxDist, float angle = 0f)
{
    public Vec2f pos;
    public float maxDist = maxDist;
    public bool fixFisheyeEffect = true;

#pragma warning disable IDE0032
    private float _angle = angle, _fov = fovRadians, _fovFactor = MathF.Tan(fovRadians/2f);
#pragma warning restore
    private Vec2f _plane = Vec2f.PlaneFromFov(angle, fovRadians), _forward = Vec2f.FromAngle(angle), _right = Vec2f.FromAngle(angle - MathF.PI/2f);


    public float angle
    {
        get => _angle;
        set {
            _angle = Utils.NormAngle(value);
            _forward = Vec2f.FromAngle(_angle);
        }
    }
    public float angleDeg
    {
        get => angle * Utils.Rad2Deg;
        set => angle = value * Utils.Deg2Rad;
    }
    public float fov
    {
        get => _fov;
        set { 
            _fov = value;
            _fovFactor = MathF.Tan(_fov/2f);
            _plane = Vec2f.PlaneFromFovFactor(forward, fovFactor);
        }
    }
    public float fovDeg
    {
        get => _fov * Utils.Rad2Deg;
        set => _fov = value * Utils.Deg2Rad;
    }
    public Vec2f forward => _forward;
    public Vec2f right => _right;
    public float fovFactor => _fovFactor;
    public Vec2f plane => _plane;


    public void RotateRad(float rad)
        => angle += rad;

    public void RotateDeg(float deg)
        => angleDeg += deg;
}