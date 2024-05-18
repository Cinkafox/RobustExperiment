using System.Numerics;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public struct Triangle
{
    public DrawVertexUV3D p1;
    public DrawVertexUV3D p2;
    public DrawVertexUV3D p3;
    public Texture Texture;

    public Triangle(DrawVertexUV3D p1, DrawVertexUV3D p2, DrawVertexUV3D p3, Texture texture)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        Texture = texture;
    }

    public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        p1 = new DrawVertexUV3D(v1, Vector2.Zero);
        p2 = new DrawVertexUV3D(v2, new Vector2(0, 1));
        p3 = new DrawVertexUV3D(v3, Vector2.One);
        Texture = Texture.White;
    }

    public void Transform(Matrix4 matrix4)
    {
        p1.Transform(matrix4);
        p2.Transform(matrix4);
        p3.Transform(matrix4);
    }

    public Vector3 Normal()
    {
        var u = new Vector3(p2.Position.X - p1.Position.X, p2.Position.Y - p1.Position.Y, p2.Position.Z - p1.Position.Z);
        var v = new Vector3(p3.Position.X - p1.Position.X, p3.Position.Y - p1.Position.Y, p3.Position.Z - p1.Position.Z);

        float nx = u.Y * v.Z - u.Z * v.Y;
        float ny = u.Z * v.X - u.X * v.Z;
        float nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }
}