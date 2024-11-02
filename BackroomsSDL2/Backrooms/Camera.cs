using System;
using Backrooms.Extensions;

namespace Backrooms;

public class Camera(float fovRad, float renderDist, Vec2f pos = default, float angle = 0f)
{
    public Vec2f pos = pos;
    public float renderDist = renderDist;


    public float fovFactor { get; private set; } = MathF.Tan(fovRad / 2f);
    public Vec2f plane { get; private set; } = Vec2f.PlaneFromFov(angle, fovRad);
    public Vec2f forward { get; private set; } = Vec2f.FromAngle(angle);
    public Vec2f right { get; private set; } = Vec2f.FromAngle(angle - MathF.PI/2f);

    private float _angle = angle;
    public float angle
    {
        get => _angle;
        set
        {
            _angle = value.NormAngle();
            forward = Vec2f.FromAngle(value);
            right = Vec2f.FromAngle(value + MathF.PI/2f); // ANGLEFLIP
            plane = Vec2f.PlaneFromFovFactor(forward, fovFactor);
        }
    }

    private float _fov = fovRad;
    public float fov
    {
        get => _fov;
        set
        {
            _fov = value;
            fovFactor = MathF.Tan(value / 2f);
            plane = Vec2f.PlaneFromFovFactor(fov, fovFactor);
        }
    }
}