using System.Numerics;

namespace Content.Client.Utils;

public sealed class TexturedTriangle
{
    public readonly Triangle Triangle = new();
    public Vector2 TexturePoint1;
    public Vector2 TexturePoint2;
    public Vector2 TexturePoint3;
    public int TextureId;

    public void Clear()
    {
        Triangle.Clear();
        TexturePoint1 = TexturePoint2 = TexturePoint3 = default;
    }

    public void TransformTexture()
    {
        TexturePoint1.X /= Triangle.p1.W;
        TexturePoint2.X /= Triangle.p2.W;
        TexturePoint3.X /= Triangle.p3.W;
        TexturePoint1.Y /= Triangle.p1.W;
        TexturePoint2.Y /= Triangle.p2.W;
        TexturePoint3.Y /= Triangle.p3.W;
    }
}