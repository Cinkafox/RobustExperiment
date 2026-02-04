using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using PlaneShape = Content.Shared.Physics.Shapes.PlaneShape;
using SphereShape = Content.Shared.Physics.Shapes.SphereShape;

namespace Content.Shared.Physics.Colliders;

[ColliderRegister(typeof(SphereShape), typeof(SphereShape))]
public sealed class SphereCollider : ICollider<SphereShape, SphereShape>
{
    private const float Epsilon = 0.0001f;
    
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<SphereShape> a, 
        TransformedPhysicShape<SphereShape> b)
    {
        var aCenter = a.Position;
        var bCenter = b.Position;
        var ab = bCenter - aCenter;
        var distanceSq = ab.LengthSquared();
    
        if (distanceSq < 1e-8f) 
            return ManifoldPoints.Empty;
    
        var distance = MathF.Sqrt(distanceSq);
        var aRadius = a.Shape.Radius * MathF.Max(a.Scale.X, MathF.Max(a.Scale.Y, a.Scale.Z));
        var bRadius = b.Shape.Radius * MathF.Max(b.Scale.X, MathF.Max(b.Scale.Y, b.Scale.Z));
        var radiusSum = aRadius + bRadius;
        
        if (distance > radiusSum)
            return ManifoldPoints.Empty;
        
        var penetrationDepth = radiusSum - distance;
        
        var normal = ab / distance;
        
        var pointA = aCenter + normal * aRadius;
        var pointB = bCenter - normal * bRadius;
    
        return new ManifoldPoints(pointA, pointB, normal, penetrationDepth);
    }
}

[ColliderRegister(typeof(PlaneShape), typeof(SphereShape))]
public sealed class PlaneSphereCollider : ICollider<PlaneShape, SphereShape>
{
    private const float Epsilon = 0.0001f;
    
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<PlaneShape> plane,
        TransformedPhysicShape<SphereShape> sphere)
    {
        var normal = Vector3.Normalize(plane.Rotation.RotateVec(plane.Shape.Normal));
        
        var sphereToPlane = sphere.Position - plane.Position;
        var distanceToPlane = Vector3.Dot(sphereToPlane, normal) - plane.Shape.Distance;
        
        var sphereRadius = sphere.Shape.Radius * MathF.Max(sphere.Scale.X, MathF.Max(sphere.Scale.Y, sphere.Scale.Z));
        
        if (distanceToPlane > sphereRadius)
            return ManifoldPoints.Empty;

        var penetrationDepth = sphereRadius - distanceToPlane;
        
        var collisionNormal = -normal; 
        
        var sphereContact = sphere.Position - collisionNormal * sphereRadius;
        var planeContact = sphere.Position - collisionNormal * distanceToPlane;
        
        return new ManifoldPoints(planeContact, sphereContact, normal, penetrationDepth);
    }
}