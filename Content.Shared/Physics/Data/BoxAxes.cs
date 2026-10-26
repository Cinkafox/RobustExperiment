namespace Content.Shared.Physics.Data;

public readonly struct BoxAxes
{
    public readonly Vector3 X;
    public readonly Vector3 Y;
    public readonly Vector3 Z;

    public BoxAxes(Vector3 x, Vector3 y, Vector3 z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3 this[int index]
    {
        get
        {
            return index switch
            {
                0 => X,
                1 => Y,
                _ => Z
            };
        }
    }
}