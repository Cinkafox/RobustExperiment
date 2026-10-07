using System.Numerics;

namespace Content.Client.Utils;

public sealed class TexturedTriangle
{
    public readonly Triangle Triangle = new();

    /// <summary>
    ///     Texture coordinates of the vertex.
    ///     X/Y are the UV; Z is the perspective denominator:
    ///     1 before projection, 1/w after projection.
    /// </summary>
    public Vector3 TexturePoint1;
    public Vector3 TexturePoint2;
    public Vector3 TexturePoint3;
    public int TextureId;

    public void Clear()
    {
        Triangle.Clear();
        TexturePoint1 = TexturePoint2 = TexturePoint3 = new Vector3(0f, 0f, 1f);
        TextureId = 0;
    }

    public void TransformTexture()
    {
        TexturePoint1.X /= Triangle.p1.W;
        TexturePoint2.X /= Triangle.p2.W;
        TexturePoint3.X /= Triangle.p3.W;
        TexturePoint1.Y /= Triangle.p1.W;
        TexturePoint2.Y /= Triangle.p2.W;
        TexturePoint3.Y /= Triangle.p3.W;
        TexturePoint1.Z /= Triangle.p1.W;
        TexturePoint2.Z /= Triangle.p2.W;
        TexturePoint3.Z /= Triangle.p3.W;
    }
}
