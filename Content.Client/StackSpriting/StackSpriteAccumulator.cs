using System.Numerics;
using Robust.Client.Graphics;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.StackSpriting;

public sealed class StackSpriteAccumulator
{
    public readonly SimpleBuffer<Vector3> Vertexes = new(1024*256);
    public readonly SimpleBuffer<Robust.Client.Graphics.Texture> TexturePool = new(1024*256);
    
    public readonly DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[6];
    public readonly Vector2[] DebugVertexes = new Vector2[4];

    public SortedDictionary<int, List<(int,int)>> DrawQueue = new();
    public int MaxHeight = 0;
}