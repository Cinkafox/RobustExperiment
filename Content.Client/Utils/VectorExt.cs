namespace Content.Client.Utils;

public static class VectorExt
{
    public static Vector4 ToVec4(this Vector3 vector3)
    {
        return new Vector4(vector3, 1);
    }

    public static Vector3 ToVec3(this Vector4 vector4)
    {
        return new Vector3(vector4.X, vector4.Y, vector4.Z);
    }
}