using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Colliders;

[ColliderRegister(typeof(PlaneShape), typeof(PlaneShape))]
public sealed class PlaneCollider : ICollider<PlaneShape, PlaneShape>
{
    public ManifoldPoints ProcessCollision(TransformedPhysicShape<PlaneShape> a, TransformedPhysicShape<PlaneShape> b)
    {
        return ManifoldPoints.Empty;
    }
}

public sealed class RevCollider : ICollider
{
    private readonly object _collider;

    public RevCollider(object collider)
    {
        _collider = collider;
    }

    public ManifoldPoints ProcessCollision(TransformedPhysicShape a, TransformedPhysicShape b)
    {
        return ((ICollider)_collider).ProcessCollision(b, a);
    }
}