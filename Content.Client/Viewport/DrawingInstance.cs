using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Content.Shared.Thread;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.Viewport;

public sealed class DrawingInstance
{
    public readonly SimpleBuffer<TexturedTriangle> TriangleBuffer = new(8192*128);
    public readonly Queue<TexturedTriangle> ListTriangles = new();

    public readonly SimpleBuffer<Texture> TextureBuffer = new(64*32);
    public readonly Vector3[] DrawVertex3dBuffer = new Vector3[3];
    public readonly Vector2[] DrawVertexUntexturedBuffer = new Vector2[3];
    public readonly Vector2[] DrawVertexTexturePointBuffer = new Vector2[3];
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public readonly SandboxCreator<ClippingInstance> ClipCreator = new();
    public readonly ShaderCreator ShaderCreator;

    public readonly ClippingInstance ClippingInstance = new();
    public readonly SimpleBuffer<ShaderInstance> ShadersPool;

    public readonly ShaderInstance ShaderInstance;
    
    public static readonly TriangleZComparer TriangleZComparer = new();

    public DrawingInstance()
    {
        ShaderInstance = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("ZDepthShader").InstanceUnique();
        ShaderCreator = new ShaderCreator(ShaderInstance);

        ShadersPool = new SimpleBuffer<ShaderInstance>(1024*128);
        for (var i = 0; i < ShadersPool.Buffer.Length; i++)
        {
            ShadersPool.Buffer[i] = ShaderCreator.Create();
        }

        for (var i = 0; i < TriangleBuffer.Buffer.Length; i++)
        {
            TriangleBuffer.Buffer[i] = new TexturedTriangle();
        }
    }

    public void AppendTriangle(TexturedTriangle triangle)
    {
        TriangleBuffer.Add(triangle);
    }
    
    public void PrepareFrame()
    {
        TriangleBuffer.Sort(TriangleZComparer);
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

public sealed class TriangleZComparer : IComparer<TexturedTriangle>
{
    public int Compare(TexturedTriangle? a, TexturedTriangle? b)
    {
        if (a is null || b is null) return 0;
        
        return a.Triangle.Z.CompareTo(b.Triangle.Z);
    }
}