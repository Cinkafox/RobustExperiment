using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public readonly Queue<TexturedTriangle> ListTriangles = new();
    
    public readonly Vector3[] DrawVertex3dBuffer = new Vector3[3];
    public readonly Vector2[] DrawVertexUntexturedBuffer = new Vector2[3];
    public readonly Vector2[] DrawVertexTexturePointBuffer = new Vector2[3];
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public readonly ClippingInstance ClippingInstance = new();
    public readonly SimplePool<ShaderInstance> ShadersPool;
    public readonly ShaderInstance ShaderInstance;

    public readonly SimplePool<TexturedTriangle> TriangleBuffer = new(8192*128, () => new TexturedTriangle());
    public readonly SimplePool<Triangle> DebugTriangleBuffer = new(8192*32, () => new Triangle());
    public readonly SimpleBuffer<Texture> TextureBuffer = new(64*32);
    
    private readonly SimpleBuffer<TexturedTriangle> _drawnBuffer = new(8192*128);
    private static readonly TriangleZComparer TriangleZComparer = new();

    public DrawingInstance(IPrototypeManager prototypeManager)
    {
        ShaderInstance = prototypeManager.Index<ShaderPrototype>("ZDepthShader").InstanceUnique();
        ShadersPool = new SimplePool<ShaderInstance>(1024*128, () => ShaderInstance.Duplicate(), true);

        FillArrays(ref DrawVertex3dBuffer, () => new Vector3());
        FillArrays(ref DrawVertexUntexturedBuffer, () => new Vector2());
        FillArrays(ref DrawVertexTexturePointBuffer, () => new Vector2());
        FillArrays(ref DrawVertexBuffer, () => new DrawVertexUV2D(Vector2.Zero, Vector2.Zero));
    }

    private void FillArrays<T>(ref T[] array, Func<T> instance)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = instance();
        }
    }

    public void AddTriangleDrawn(TexturedTriangle triangle)
    {
        _drawnBuffer.Add(triangle);
    }

    public IEnumerable<TexturedTriangle> EnumerateDrawnTriangles()
    {
        return _drawnBuffer;
    }

    public int GetDrawnTriangles()
    {
        return _drawnBuffer.Length;
    }
    
    public void PrepareFrame()
    {
        _drawnBuffer.Sort(TriangleZComparer);
    }

    public int AllocTexture(List<Material> materials)
    {
        var currLength = TextureBuffer.Length;
        foreach (var material in materials)
        {
            TextureBuffer.Add(material.MapKd?.Value ?? CreateBlankTexture(new Color(material.Kd.X, material.Kd.Y, material.Kd.Z)));
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
        DebugTriangleBuffer.Clear();
        _drawnBuffer.Clear();
        ShadersPool.Clear();
    }
}

public sealed class TriangleZComparer : IComparer<TexturedTriangle>
{
    public int Compare(TexturedTriangle? a, TexturedTriangle? b)
    {
        if (a is null || b is null) return 0;
        
        return a.Triangle.Z.CompareTo(b.Triangle.Z);
    }
}