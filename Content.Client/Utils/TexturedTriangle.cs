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
}