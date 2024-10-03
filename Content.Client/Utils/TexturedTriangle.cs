using System.Collections;
using System.Numerics;

namespace Content.Client.Utils;

public struct TexturedTriangle
{
    public Triangle Triangle;
    public Vector2 TexturePoint1;
    public Vector2 TexturePoint2;
    public Vector2 TexturePoint3;
    public int TextureId;

    public TexturedTriangle(Triangle triangle, Vector2 p1, Vector2 p2, Vector2 p3, int textureId)
    {
        Triangle = triangle;
        TexturePoint1 = p1;
        TexturePoint2 = p2;
        TexturePoint3 = p3;
        TextureId = textureId;
    }
}