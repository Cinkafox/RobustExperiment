using System.Numerics;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public struct DrawVertexUV3D
{
    public Vector3 Position;
    public Vector2 UV;

    public DrawVertexUV3D(Vector3 position, Vector2 uv)
    {
        Position = position;
        UV = uv;
    }

    public void Transform(Matrix4 matrix4)
    {
        Position = Vector3.Transform(Position, matrix4);
    }
}