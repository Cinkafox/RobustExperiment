namespace Content.Client.Viewport;

public struct CameraProperties
{
    public Vector3 Position;
    public Angle3d Angle;
    public float FoV;
    public Vector3 CameraDirection {
        get
        {
            var pitch = Angle.Pitch.ToVec();
            var yaw = Angle.Yaw.ToVec();
            return new Vector3(pitch.Y , 0, pitch.X);
        }
    }
    public CameraProperties(Vector3 position, Angle3d angle, float foV)
    {
        Position = position;
        Angle = angle;
        FoV = foV;
    }
}