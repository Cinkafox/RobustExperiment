using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class SphereShape : IPhysicShape
{
    [DataField] public float Radius = 1f;
    public float Area => 4 * float.Pi * Radius * Radius;
    
    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape)
    {
        var realRadius = transformedPhysicShape.Scale * Radius;
        var pos = transformedPhysicShape.Position;
        
        handle.DrawSphere(pos, realRadius.X);
    }
}