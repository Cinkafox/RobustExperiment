using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Client.Utils;
using Content.Shared.Thread;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
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
    public readonly Robust.Shared.Maths.Vector3[] DrawVertex3dBuffer = new Robust.Shared.Maths.Vector3[3];
    public readonly Vector2[] DrawVertexUntexturedBuffer = new Vector2[3];
    public readonly Vector2[] DrawVertexTexturePointBuffer = new Vector2[3];
    public readonly DrawVertexUV2D[] DrawVertexBuffer = new DrawVertexUV2D[3];

    public readonly SandboxCreator<ClippingInstance> ClipCreator = new();
    public readonly ShaderCreator ShaderCreator;

    public readonly ClippingInstance ClippingInstance = new();
    public readonly ThreadPool<ClippingInstance> AsyncClippingInstances;
    public readonly SimpleBuffer<ShaderInstance> ShadersPool;

    public readonly ShaderInstance ShaderInstance;

    public DrawingInstance()
    {
        AsyncClippingInstances = new(128*2, ClipCreator);
        
        ShaderInstance = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("ZDepthShader").InstanceUnique();
        ShaderCreator = new ShaderCreator(ShaderInstance);

        ShadersPool = new SimpleBuffer<ShaderInstance>(1024*10);
        for (int i = 0; i < ShadersPool.Buffer.Length; i++)
        {
            ShadersPool.Buffer[i] = ShaderCreator.Create();
        }
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