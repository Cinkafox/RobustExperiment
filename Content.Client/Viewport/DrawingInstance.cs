using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public readonly SortedDictionary<float,TexturedTriangle> TriangleBuffer = new();
    public readonly List<TexturedTriangle> ListTriangles = new();
    
    public readonly SimpleBuffer<Texture> TextureBuffer = new(64*3);
    public readonly Vector3[] DrawVertex3dBuffer = new Vector3[3];
    public readonly Vector2[] DrawVertexUntexturedBuffer = new Vector2[3];
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public readonly ClippingInstance ClippingInstance = new();

    public readonly TriangleJob Job;

    public DrawingInstance()
    {
        Job = new TriangleJob(this);
    }

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
            TextureBuffer.Add(material.MapKd ?? CreateBlankTexture(new Color(material.Kd.X, material.Kd.Y, material.Kd.Z)));
        }

        return currLength;
    }

    public Texture CreateBlankTexture(Color color)
    {
        var clyde = IoCManager.Resolve<IClyde>();
        var img = new Image<Rgba32>(128, 128, color.ConvertImgSharp());
        return clyde.LoadTextureFromImage(img);
    }

    public void Flush()
    {
        TriangleBuffer.Clear();
    }
}

public sealed class ClippingInstance
{
    public readonly SimpleBuffer<Vector3> InsidePoints = new(3);
    public readonly SimpleBuffer<Vector3> OutsidePoints = new(3);
    public readonly SimpleBuffer<Vector2> InsideTex = new(3);
    public readonly SimpleBuffer<Vector2> OutsideTex = new(3);
    public readonly SimpleBuffer<TexturedTriangle> Clipping = new(2);

    public void Clear()
    {
        InsidePoints.Clear();
        OutsidePoints.Clear();
        InsideTex.Clear();
        OutsideTex.Clear();
        Clipping.Clear();
    }
}

public sealed class TriangleJob : IParallelRobustJob
{
    private DrawingInstance _drawingInstance;
    public DrawingHandle3d DrawingHandle3d = default!;

    public TriangleJob(DrawingInstance drawingInstance)
    {
        _drawingInstance = drawingInstance;
    }

    public void Execute(int i)
    {
        var texTriangle = new TexturedTriangle(new Triangle(Vector3.Zero, Vector3.Zero, Vector3.Zero), Vector2.Zero,Vector2.Zero, Vector2.Zero, 0);
        
        texTriangle.Triangle.p1 = Vector3.Transform(_drawingInstance.ClippingInstance.Clipping[i].Triangle.p1, DrawingHandle3d.ProjectionMatrix);
        texTriangle.Triangle.p2 = Vector3.Transform(_drawingInstance.ClippingInstance.Clipping[i].Triangle.p2, DrawingHandle3d.ProjectionMatrix);
        texTriangle.Triangle.p3 = Vector3.Transform(_drawingInstance.ClippingInstance.Clipping[i].Triangle.p3, DrawingHandle3d.ProjectionMatrix);
        texTriangle.TexturePoint1 = _drawingInstance.ClippingInstance.Clipping[i].TexturePoint1;
        texTriangle.TexturePoint2 = _drawingInstance.ClippingInstance.Clipping[i].TexturePoint2;
        texTriangle.TexturePoint3 = _drawingInstance.ClippingInstance.Clipping[i].TexturePoint3;

        texTriangle.TexturePoint1.X /= texTriangle.Triangle.p1w;
        texTriangle.TexturePoint2.X /= texTriangle.Triangle.p2w;
        texTriangle.TexturePoint3.X /= texTriangle.Triangle.p3w;
        texTriangle.TexturePoint1.Y /= texTriangle.Triangle.p1w;
        texTriangle.TexturePoint2.Y /= texTriangle.Triangle.p2w;
        texTriangle.TexturePoint3.Y /= texTriangle.Triangle.p3w;
            
        texTriangle.Triangle.p1.X *= -1;
        texTriangle.Triangle.p2.X *= -1;
        texTriangle.Triangle.p3.X *= -1;
        texTriangle.Triangle.p1.Y *= -1;
        texTriangle.Triangle.p2.Y *= -1;
        texTriangle.Triangle.p3.Y *= -1;
            
        texTriangle.Triangle.p1 = DrawingHandle3d.ToScreenVec(texTriangle.Triangle.p1);
        texTriangle.Triangle.p2 = DrawingHandle3d.ToScreenVec(texTriangle.Triangle.p2);
        texTriangle.Triangle.p3 = DrawingHandle3d.ToScreenVec(texTriangle.Triangle.p3);
            
        lock (_drawingInstance.TriangleBuffer)
        {
            _drawingInstance.AppendTriangle(texTriangle);
        }
    }
}