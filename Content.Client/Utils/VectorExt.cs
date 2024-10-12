namespace Content.Client.Utils;

public static class VectorExt
{
    public static Vector3 IntersectPlane(Vector3 plane_p, Vector3 plane_n, Vector3 lineStart, Vector3 lineEnd)
    {
        plane_n = Vector3.Normalize(plane_n);
        
        float plane_d = -Vector3.Dot(plane_n, plane_p);
        float ad = Vector3.Dot(lineStart, plane_n);
        float bd = Vector3.Dot(lineEnd, plane_n);
        float t = (-plane_d - ad) / (bd - ad);
        var lineStartToEnd = Vector3.Subtract(lineEnd, lineStart);
        var lineToIntersect = Vector3.Multiply(lineStartToEnd, t);
        return Vector3.Add(lineStart, lineToIntersect);
    }

    public static Vector4 ToVec4(this Vector3 vector3)
    {
        return new Vector4(vector3, 1);
    }

    public static Vector3 ToVec3(this Vector4 vector4)
    {
        return new Vector3(vector4.X, vector4.Y, vector4.Z);
    }
}