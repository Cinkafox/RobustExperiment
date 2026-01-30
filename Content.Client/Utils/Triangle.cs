using System.Collections;
using System.Numerics;
using Content.Client.Viewport;

namespace Content.Client.Utils;

public sealed class Triangle : IEnumerable<Vector3>
{
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;

    public float p1w = 1f;
    public float p2w = 1f;
    public float p3w = 1f;

    public float Z => (p1.Z + p2.Z + p3.Z) / 3;

    public void Transform(Matrix4x4 matrix4)
    {
        p1 = Vector3.Transform(p1, matrix4);
        p2 = Vector3.Transform(p2, matrix4);
        p3 = Vector3.Transform(p3, matrix4);
    }

    public Vector3 Normal()
    {
        var u = new Vector3(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        var v = new Vector3(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);

        var nx = u.Y * v.Z - u.Z * v.Y;
        var ny = u.Z * v.X - u.X * v.Z;
        var nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }

    public void Clear()
    {
        p1 = p2 = p3 = default;
        p1w = p2w = p3w = 1f;
    }

    public IEnumerator<Vector3> GetEnumerator()
    {
        yield return p1;
        yield return p2;
        yield return p3;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}