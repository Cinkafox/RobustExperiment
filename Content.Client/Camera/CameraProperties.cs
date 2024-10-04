using Content.Shared.Utils;

namespace Content.Client.Camera;

public struct CameraProperties
{
    public Vector3 Position;
    public Angle3d Angle;
    public float FoV;
    
    public CameraProperties(Vector3 position, Angle3d angle, float foV)
    {
        Position = position;
        Angle = angle;
        FoV = foV;
    }
}