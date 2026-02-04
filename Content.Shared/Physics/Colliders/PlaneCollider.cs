using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using PlaneShape = Content.Shared.Physics.Shapes.PlaneShape;

namespace Content.Shared.Physics.Colliders;

[ColliderRegister(typeof(PlaneShape), typeof(PlaneShape))]
public sealed class PlanePlaneCollider : ICollider<PlaneShape, PlaneShape>
{
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<PlaneShape> a, 
        TransformedPhysicShape<PlaneShape> b)
    {
        var normalA = Vector3.Normalize(a.Rotation.RotateVec(a.Shape.Normal));
        var normalB = Vector3.Normalize(b.Rotation.RotateVec(b.Shape.Normal));
        
        var dot = MathF.Abs(Vector3.Dot(normalA, normalB));
        if (dot < 0.999f) // Not parallel enough
            return ManifoldPoints.Empty;
        
        return ManifoldPoints.Empty;
    }
}
