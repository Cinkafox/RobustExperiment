namespace Content.Client.Viewport;

public struct CameraProperties
{
    public Vector3 Position;
    public Angle3d Angle;
    public float FoV;
    public Vector3 CameraDirection {
        get
        {
            var target = Vector3.UnitZ;
            var matCamRot = Matrix4.CreateRotationY((float)Angle.Yaw);
            return Vector3.Transform(target, matCamRot);
        }
    }
    public CameraProperties(Vector3 position, Angle3d angle, float foV)
    {
        Position = position;
        Angle = angle;
        FoV = foV;
    }
}