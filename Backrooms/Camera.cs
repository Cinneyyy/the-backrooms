using System;

namespace Backrooms;

public class Camera(float fovDegrees, float maxDist, float angle = 0f)
{
    public Vec2f pos;
    public float maxDist = maxDist;

    private float _angle = angle, _fov = fovDegrees * Utils.Deg2Rad;
    private Vec2f _plane = Vec2f.PlaneFromFov(angle, fovDegrees * Utils.Deg2Rad), _forward = Vec2f.FromAngle(angle), _right = Vec2f.FromAngle(angle - MathF.PI/2f);


    public float angle
    {
        get => _angle;
        set {
            _angle = Utils.NormAngle(value);
            _forward = Vec2f.FromAngle(_angle);
            _right = Vec2f.FromAngle(_angle - MathF.PI/2f);
            _plane = Vec2f.PlaneFromFovFactor(forward, fovFactor);
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
            fovFactor = MathF.Tan(_fov/2f);
            _plane = Vec2f.PlaneFromFovFactor(forward, fovFactor);
        }
    }
    public float fovFactor { get; private set; } = MathF.Tan(fovDegrees * Utils.Deg2Rad / 2f);
    public float fovDeg
    {
        get => _fov * Utils.Rad2Deg;
        set => _fov = value * Utils.Deg2Rad;
    }
    public Vec2f forward => _forward;
    public Vec2f right => _right;
    public Vec2f plane => _plane;


    public void RotateRad(float rad)
        => angle += rad;

    public void RotateDeg(float deg)
        => angleDeg += deg;
}