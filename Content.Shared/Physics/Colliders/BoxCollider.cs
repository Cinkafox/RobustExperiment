using Content.Shared.Physics.Data;
using Content.Shared.Physics.Shapes;

namespace Content.Shared.Physics.Colliders;

[ColliderRegister(typeof(BoxShape), typeof(SphereShape))]
public sealed class BoxSphereCollider : ICollider<BoxShape, SphereShape>
{
    private const float Epsilon = 1e-4f;
    
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<BoxShape> box,
        TransformedPhysicShape<SphereShape> sphere)
    {
        // Get closest point on box surface to sphere center
        var closestPoint = box.Shape.GetClosestPointOnBox(box, sphere.Position);
        var toSphere = sphere.Position - closestPoint;
        var distanceSq = toSphere.LengthSquared();
        
        // No collision if too far
        var sphereRadius = sphere.Shape.Radius * MathF.Max(
            MathF.Abs(sphere.Scale.X),
            MathF.Max(MathF.Abs(sphere.Scale.Y), MathF.Abs(sphere.Scale.Z))
        );
        
        if (distanceSq > sphereRadius * sphereRadius)
            return ManifoldPoints.Empty;
        
        // Handle coincident points
        if (distanceSq < Epsilon * Epsilon)
        {
            // Use box normal facing sphere (approximate)
            var boxAxes = box.Shape.GetBoxAxes(box);
            var boxExtents = box.Shape.GetBoxHalfExtents(box);
            var localSphere = box.Rotation.Conjugate.RotateVec(sphere.Position - box.Position);
            
            // Find dominant axis
            var absLocal = new Vector3(
                MathF.Abs(localSphere.X),
                MathF.Abs(localSphere.Y),
                MathF.Abs(localSphere.Z)
            );
            
            Vector3 normalBox;
            if (absLocal.X > absLocal.Y && absLocal.X > absLocal.Z)
                normalBox = boxAxes[0] * MathF.Sign(localSphere.X);
            else if (absLocal.Y > absLocal.Z)
                normalBox = boxAxes[1] * MathF.Sign(localSphere.Y);
            else
                normalBox = boxAxes[2] * MathF.Sign(localSphere.Z);
            
            return new ManifoldPoints(
                closestPoint,
                sphere.Position - normalBox * sphereRadius,
                normalBox,
                sphereRadius
            );
        }
        
        // Normal points FROM box TO sphere (A=box, B=sphere)
        var distance = MathF.Sqrt(distanceSq);
        var normal = toSphere / distance;
        var penetrationDepth = sphereRadius - distance;
        
        // Contact points
        var boxContact = closestPoint;
        var sphereContact = sphere.Position - normal * sphereRadius;
        
        return new ManifoldPoints(boxContact, sphereContact, normal, penetrationDepth);
    }
}

[ColliderRegister(typeof(BoxShape), typeof(PlaneShape))]
public sealed class BoxPlaneCollider : ICollider<BoxShape, PlaneShape>
{
    private const float Epsilon = 1e-4f;
    
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<BoxShape> box,
        TransformedPhysicShape<PlaneShape> plane)
    {
        // Transform plane normal to world space
        var planeNormal = Vector3.Normalize(plane.Rotation.RotateVec(plane.Shape.Normal));
        
        // Get all box vertices
        var vertices = box.Shape.GetBoxVertices(box);
        
        // Find most penetrating vertex (minimum signed distance to plane)
        float minDistance = float.MaxValue;
        Vector3 mostPenetratingVertex = Vector3.Zero;
        bool anyBehind = false;
        
        foreach (var vertex in vertices)
        {
            var toPlane = vertex - plane.Position;
            var distance = Vector3.Dot(toPlane, planeNormal) - plane.Shape.Distance;
            
            if (distance < minDistance)
            {
                minDistance = distance;
                mostPenetratingVertex = vertex;
            }
            
            if (distance <= 0f)
                anyBehind = true;
        }
        
        // No collision if all vertices in front of plane
        var boxExtents = box.Shape.GetBoxHalfExtents(box);
        var maxExtent = MathF.Max(boxExtents.X, MathF.Max(boxExtents.Y, boxExtents.Z));
        
        if (!anyBehind || minDistance > maxExtent)
            return ManifoldPoints.Empty;
        
        // Penetration depth (how far vertex penetrates plane)
        var penetrationDepth = -minDistance; // Positive when behind plane
        
        // Contact points
        var planeContact = mostPenetratingVertex - planeNormal * minDistance;
        var boxContact = mostPenetratingVertex;
        
        // Normal points FROM plane TO box (A=box, B=plane → must point FROM box TO plane)
        // Since we want to push box away from plane, normal should point opposite to plane normal
        // Convention: Normal points FROM A (box) TO B (plane) → so -planeNormal
        var collisionNormal = -planeNormal;
        
        return new ManifoldPoints(boxContact, planeContact, collisionNormal, penetrationDepth);
    }
}

[ColliderRegister(typeof(BoxShape), typeof(BoxShape))]
public sealed class BoxBoxCollider : ICollider<BoxShape, BoxShape>
{
    private const float Epsilon = 1e-4f;
    private const float MaxDepth = 10f;
    
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<BoxShape> a,
        TransformedPhysicShape<BoxShape> b)
    {
        // Get box properties
        var aAxes = a.GetBoxAxes();
        var bAxes = b.GetBoxAxes();
        var aExtents = a.GetBoxHalfExtents();
        var bExtents = b.GetBoxHalfExtents();
        var aPos = a.Position;
        var bPos = b.Position;
        
        // Vector between centers
        var ab = bPos - aPos;
        
        // Test all 15 SAT axes
        float minOverlap = float.MaxValue;
        Vector3 collisionNormal = Vector3.Zero;
        bool foundAxis = false;
        
        // Helper to test a single axis
        bool TestAxis(Vector3 axis, ref float minOverlap, ref Vector3 collisionNormal)
        {
            if (axis.LengthSquared() < Epsilon * Epsilon)
                return true; // Degenerate axis
            
            axis = Vector3.Normalize(axis);
            
            // Project both boxes onto axis
            var aProj = ProjectBoxOntoAxis(aPos, aAxes, aExtents, axis);
            var bProj = ProjectBoxOntoAxis(bPos, bAxes, bExtents, axis);
            
            // Calculate overlap
            var overlap = MathF.Min(aProj.Max, bProj.Max) - MathF.Max(aProj.Min, bProj.Min);
            
            if (overlap < Epsilon) // No overlap on this axis = no collision
                return false;
            
            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                collisionNormal = axis;
                
                // Ensure normal points FROM A TO B
                if (Vector3.Dot(ab, axis) < 0f)
                    collisionNormal = -axis;
            }
            
            return true;
        }
        
        // Test A's axes
        for (var i = 0; i < 3; i++)
        {
            if (!TestAxis(aAxes[i], ref minOverlap, ref collisionNormal))
                return ManifoldPoints.Empty;
        }
        
        // Test B's axes
        for (var i = 0; i < 3; i++)
        {
            if (!TestAxis(bAxes[i], ref minOverlap, ref collisionNormal))
                return ManifoldPoints.Empty;
        }
        
        // Test cross products (edge-edge axes)
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var cross = Vector3.Cross(aAxes[i], bAxes[j]);
                if (!TestAxis(cross, ref minOverlap, ref collisionNormal))
                    return ManifoldPoints.Empty;
            }
        }
        
        // No separating axis found → collision
        if (minOverlap >= float.MaxValue || minOverlap < Epsilon)
            return ManifoldPoints.Empty;
        
        // Clamp penetration depth
        var penetrationDepth = MathF.Min(minOverlap, MaxDepth);
        
        // Find contact points (approximate - use clipping for production)
        var contactA = FindBoxSupportPoint(a, -collisionNormal);
        var contactB = FindBoxSupportPoint(b, collisionNormal);
        
        // Midpoint between contacts as representative contact point
        var avgContact = (contactA + contactB) * 0.5f;
        
        // Refine contacts to lie on collision plane
        var aContact = avgContact - collisionNormal * (penetrationDepth * 0.5f);
        var bContact = avgContact + collisionNormal * (penetrationDepth * 0.5f);
        
        return new ManifoldPoints(aContact, bContact, collisionNormal, penetrationDepth);
    }
    
    private (float Min, float Max) ProjectBoxOntoAxis(
        Vector3 center, Vector3[] axes, Vector3 extents, Vector3 axis)
    {
        // Projection of center
        var centerProj = Vector3.Dot(center, axis);
        
        // Projection of extents (sum of absolute projections of each axis * extent)
        var radius = MathF.Abs(Vector3.Dot(axes[0], axis)) * extents.X +
                     MathF.Abs(Vector3.Dot(axes[1], axis)) * extents.Y +
                     MathF.Abs(Vector3.Dot(axes[2], axis)) * extents.Z;
        
        return (centerProj - radius, centerProj + radius);
    }
    
    private Vector3 FindBoxSupportPoint(TransformedPhysicShape<BoxShape> box, Vector3 direction)
    {
        // Transform direction to box-local space
        var localDir = box.Rotation.Conjugate.RotateVec(direction);
        var extents = box.GetBoxHalfExtents();
        
        // Find vertex most in direction
        var localPoint = new Vector3(
            localDir.X > 0 ? extents.X : -extents.X,
            localDir.Y > 0 ? extents.Y : -extents.Y,
            localDir.Z > 0 ? extents.Z : -extents.Z
        );
        
        // Transform to world space
        return box.Position + box.Rotation.RotateVec(localPoint);
    }
}

public static class TransformedPhysicsShapeExtensions
{
    public static Vector3[] GetBoxAxes(this TransformedPhysicShape<BoxShape> transformedPhysicShape)
    {
        return transformedPhysicShape.Shape.GetBoxAxes(transformedPhysicShape);
    }

    public static Vector3 GetBoxHalfExtents(this TransformedPhysicShape<BoxShape> transformedPhysicShape)
    {
        return transformedPhysicShape.Shape.GetBoxHalfExtents(transformedPhysicShape);
    }
}