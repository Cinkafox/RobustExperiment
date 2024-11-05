using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Colliders;

[ColliderRegister(typeof(SphereShape), typeof(SphereShape))]
public sealed class SphereCollider : ICollider<SphereShape, SphereShape>
{
    public ManifoldPoints ProcessCollision(TransformedPhysicShape<SphereShape> a, TransformedPhysicShape<SphereShape> b)
    {
        var aCenter = a.Position;
        var bCenter = b.Position;
        
        var ab = bCenter - aCenter;

        var aRadius = a.Scale.Y * a.Shape.Radius;
        var bRadius = b.Scale.Y * b.Shape.Radius;

        var distance = ab.Length();
        
        if (    distance < 0.00001f
                || distance > aRadius + bRadius)
        {
            return ManifoldPoints.Empty;
        }
        
        var normal = Vector3.Normalize(ab);
        
        var aDeep = aCenter + normal * aRadius;
        var bDeep = bCenter - normal * bRadius;

        return new ManifoldPoints(aDeep, bDeep);
    }
}

[ColliderRegister(typeof(SphereShape), typeof(PlaneShape))]
public sealed class SpherePlaneCollider : ICollider<SphereShape, PlaneShape>
{
    public ManifoldPoints ProcessCollision(TransformedPhysicShape<SphereShape> a, TransformedPhysicShape<PlaneShape> b)
    {
        var aCenter = a.Position;
        var aRadius = a.Scale.Y * a.Shape.Radius;

        var normal = b.Rotation.RotateVec(b.Shape.Normal);
        var onPlane = normal * b.Shape.Distance + b.Position;

        var distance = Vector3.Dot(aCenter - onPlane, normal); // distance from center of sphere to plane surface

        if (distance > aRadius) {
            return ManifoldPoints.Empty;
        }
		
        var aDeep = aCenter - normal * aRadius;
        var bDeep = aCenter - normal * distance;

        return new ManifoldPoints(aDeep, bDeep, normal, distance);
    }
}