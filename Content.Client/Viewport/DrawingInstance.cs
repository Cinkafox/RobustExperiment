using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public static readonly int BuffSize = 2048 * 128;

    public readonly SimpleBuffer<TexturedTriangle> TriangleBuffer = new(BuffSize);

    public readonly SimpleBuffer<Vector4> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector4> OutsidePoints = new(3);
    public readonly SimpleBuffer<Vector2> InsideTex = new(3);
    public readonly SimpleBuffer<Vector2> OutsideTex = new(3);
    public readonly SimpleBuffer<TexturedTriangle> Clipping = new(2);
    
    public readonly SimpleBuffer<Texture> TextureBuffer = new(16);
    
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public void AppendTriangle(TexturedTriangle texturedTriangle)
    {
        //TriangleBuffer.Set((int)texturedTriangle.Triangle.p1.Z, texturedTriangle);
        TriangleBuffer.Add(texturedTriangle);
    }

    public int AddTexture(Texture texture)
    {
        TextureBuffer.Add(texture);
        return TextureBuffer.Length - 1;
    }

    public int AllocTexture(List<Material> materials)
    {
        var currLength = TextureBuffer.Length;
        foreach (var material in materials)
        {
            TextureBuffer.Add(material.MapKd!);
        }

        return currLength;
    }
    
    public void Sort()
    {
        Array.Sort(TriangleBuffer.Buffer, (t1,t2) =>
        {
            var sa = (t1.Triangle.p1.Z + t1.Triangle.p2.Z + t1.Triangle.p3.Z) / 3;
            var ba = (t2.Triangle.p1.Z + t2.Triangle.p2.Z + t2.Triangle.p3.Z) / 3;
            return sa.CompareTo(ba);
        });
    }

    public void Flush()
    {
        TriangleBuffer.Clear();
    }
}