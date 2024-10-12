using Content.Shared.Utils;

namespace Content.Client.Camera;

public struct CameraProperties
{
    public Vector3 Position;
    public EulerAngles Angle;
    public float FoV;
    
    public CameraProperties(Vector3 position, EulerAngles angle, float foV)
    {
        Position = position;
        Angle = angle;
        FoV = foV;
    }
}