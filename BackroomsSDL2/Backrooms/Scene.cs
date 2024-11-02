using Backrooms.Lighting;

namespace Backrooms;

public class Scene
{
    public Camera cam;
    public CameraController camController;
    public Map map;
    public FogSettings fog;
    public LightingSettings lighting;


    public static Scene current { get; set; }
}