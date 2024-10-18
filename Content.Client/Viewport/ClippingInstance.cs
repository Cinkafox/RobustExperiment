using System.Numerics;
using Content.Client.Utils;

namespace Content.Client.Viewport;

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