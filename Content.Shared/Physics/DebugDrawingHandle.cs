using System.Linq;

namespace Content.Shared.Physics;

public sealed class DebugDrawingHandle
{
    public List<List<Vector4>> VertexBuffer { get; } = [];
    public List<(float, Vector3)> SphereBuffer { get; } = [];
    public List<(Vector3 Start, Vector3 End)> LineBuffer { get; } = [];

    public void DrawVertex(IEnumerable<Vector4> vertex)
    {
        VertexBuffer.Add(vertex.ToList());
    }
    
    public void DrawVertex(IEnumerable<Vector3> vertex)
    {
        VertexBuffer.Add(vertex.Select(a => new Vector4(a, 1f)).ToList());
    }

    public void DrawSphere(Vector3 center, float radius)
    {
        SphereBuffer.Add((radius, center));
    }

    public void DrawLine(Vector3 start, Vector3 end)
    {
        LineBuffer.Add((start, end));
    }
}