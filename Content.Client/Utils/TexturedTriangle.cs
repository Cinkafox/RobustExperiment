using System.Numerics;

namespace Content.Client.Utils;

public sealed class TexturedTriangle
{
    public readonly Triangle Triangle = new();
    
    public Vector3 TexturePoint1 = new(0,0,1);
    public Vector3 TexturePoint2 = new(0,0,1);
    public Vector3 TexturePoint3 = new(0,0,1);
    public Vector3 Normal = Vector3.Zero;
    public int TextureId;

    public void SetTexturePoints(in Vector2 p1,in Vector2 p2,in Vector2 p3)
    {
        TexturePoint1.X = p1.X;
        TexturePoint1.Y = p1.Y;
        TexturePoint1.Z = 1f;
        TexturePoint2.X = p2.X;
        TexturePoint2.Y = p2.Y;
        TexturePoint2.Z = 1f;
        TexturePoint3.X = p3.X;
        TexturePoint3.Y = p3.Y;
        TexturePoint3.Z = 1f;
    }

    public void Clear()
    {
        Triangle.Clear();
        TexturePoint1.X = 0;
        TexturePoint1.Y = 0;
        TexturePoint1.Z = 1;
        TexturePoint2.X = 0;
        TexturePoint2.Y = 0;
        TexturePoint2.Z = 1;
        TexturePoint3.X = 0;
        TexturePoint3.Y = 0;
        TexturePoint3.Z = 1;
        Normal = Vector3.Zero;
        
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
