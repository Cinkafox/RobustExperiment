using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Shapes;

[ImplicitDataDefinitionForInheritors]
public partial interface IPhysicShape
{
    [ViewVariables(VVAccess.ReadOnly)] public float Area { get; }
    
    public Vector3 CalculateLocalInertia(float mass);
    
    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape);
}