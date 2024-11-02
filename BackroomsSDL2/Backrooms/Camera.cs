using System;
using Backrooms.Extensions;

namespace Backrooms;

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class Camera
{
    public const float DEFAULT_FOV = 90f * MathExtension.Deg2Rad;
    public const float DEFAULT_RENDER_DIST = 20f;


    public static Vec2f pos;
    public static float renderDist = 20f;


    public static float fovFactor { get; private set; } = MathF.Tan(DEFAULT_FOV / 2f);
    public static Vec2f plane { get; private set; } = Vec2f.PlaneFromFov(0f, DEFAULT_FOV);
    public static Vec2f forward { get; private set; } = Vec2f.FromAngle(0f);
    public static Vec2f right { get; private set; } = Vec2f.FromAngle(0f + MathF.PI/2f); // ANGLEFLIP

    private static float _angle;
    public static float angle
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

    private static  float _fov = DEFAULT_FOV;
    public static float fov
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