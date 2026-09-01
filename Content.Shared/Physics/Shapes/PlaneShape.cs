using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class PlaneShape : IPhysicShape
{
    [DataField] public float Distance = 1f;
    [DataField] public Vector3 Normal = Vector3.UnitY;
    
    public float Area => 1f; 

    public Vector3 CalculateLocalInertia(float mass)
    {
        return Vector3.Zero;
    }

    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape)
    {
        var normal = Vector3.Transform(Normal, transformedPhysicShape.Rotation);
        var center = transformedPhysicShape.Position + normal * Distance;
        
        var tangent = MathF.Abs(normal.Y) < 0.99f
            ? Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitY))
            : Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitX));
        var bitangent = Vector3.Cross(normal, tangent);
        
        const float size = 5f;
        var p1 = center - tangent * size - bitangent * size;
        var p2 = center + tangent * size - bitangent * size;
        var p3 = center + tangent * size + bitangent * size;
        var p4 = center - tangent * size + bitangent * size;
        
        handle.DrawVertex([p1, p2, p3, p4, p1, p3, p2, p4]);
    }
}