using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public readonly SortedList<float,TexturedTriangle> TriangleBuffer = new();

    public readonly List<TexturedTriangle> ListTriangles = new();
    public readonly SimpleBuffer<Vector4> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector4> OutsidePoints = new(3);
    public readonly SimpleBuffer<Vector2> InsideTex = new(3);
    public readonly SimpleBuffer<Vector2> OutsideTex = new(3);
    public readonly SimpleBuffer<TexturedTriangle> Clipping = new(2);
    
    public readonly SimpleBuffer<Texture> TextureBuffer = new(16);
    
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public void AppendTriangle(TexturedTriangle texturedTriangle)
    {
        var z = texturedTriangle.Triangle.Z;

        while (!TriangleBuffer.TryAdd(z, texturedTriangle))
        {
            z += 0.01f;
        }
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
        
    }

    public void Flush()
    {
        TriangleBuffer.Clear();
    }
}