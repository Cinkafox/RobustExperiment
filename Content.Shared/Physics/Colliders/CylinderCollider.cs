using Content.Shared.Physics.Data;
using Content.Shared.Physics.Shapes;

namespace Content.Shared.Physics.Colliders;

public static class CylinderShapeExtensions
{
    public static Vector3 GetCylinderAxis(this TransformedPhysicShape<CylinderShape> cylinder)
    {
        var axis = cylinder.RotateVector(Vector3.UnitY);
        return axis.LengthSquared() > 1e-8f ? Vector3.Normalize(axis) : Vector3.UnitY;
    }

    public static float GetCylinderRadius(this TransformedPhysicShape<CylinderShape> cylinder)
    {
        return cylinder.Shape.Radius * MathF.Max(
            MathF.Abs(cylinder.Scale.X),
            MathF.Abs(cylinder.Scale.Z));
    }

    public static float GetCylinderHalfHeight(this TransformedPhysicShape<CylinderShape> cylinder)
    {
        return cylinder.Shape.HalfHeight * MathF.Abs(cylinder.Scale.Y);
    }

    public static Vector3[] GetCylinderBoxAxes(this TransformedPhysicShape<CylinderShape> cylinder)
    {
        return
        [
            Vector3.Normalize(cylinder.RotateVector(Vector3.UnitX)),
            cylinder.GetCylinderAxis(),
            Vector3.Normalize(cylinder.RotateVector(Vector3.UnitZ))
        ];
    }
}

[ColliderRegister(typeof(CylinderShape), typeof(SphereShape))]
public sealed class CylinderSphereCollider : ICollider<CylinderShape, SphereShape>
{
    private const float Epsilon = 1e-4f;

    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<CylinderShape> cylinder,
        TransformedPhysicShape<SphereShape> sphere)
    {
        var halfHeight = cylinder.GetCylinderHalfHeight();
        var radius = cylinder.GetCylinderRadius();

        var sphereRadius = sphere.Shape.Radius * MathF.Max(
            MathF.Abs(sphere.Scale.X),
            MathF.Max(MathF.Abs(sphere.Scale.Y), MathF.Abs(sphere.Scale.Z)));

        var local = cylinder.RotateConjugate(sphere.Position - cylinder.Position);

        var clampedY = Math.Clamp(local.Y, -halfHeight, halfHeight);
        var radial = new Vector3(local.X, 0f, local.Z);
        var radialLength = radial.Length();

        if (radialLength > radius)
            radial = radial / radialLength * radius;

        var closest = cylinder.Position + cylinder.RotateVector(new Vector3(radial.X, clampedY, radial.Z));
        var toSphere = sphere.Position - closest;
        var distanceSq = toSphere.LengthSquared();

        if (distanceSq > sphereRadius * sphereRadius)
            return ManifoldPoints.Empty;

        if (distanceSq < Epsilon * Epsilon)
        {
            Vector3 fallbackLocal;
            if (MathF.Abs(local.Y) >= radialLength)
                fallbackLocal = new Vector3(0f, local.Y >= 0f ? 1f : -1f, 0f);
            else if (radialLength > Epsilon)
                fallbackLocal = new Vector3(local.X / radialLength, 0f, local.Z / radialLength);
            else
                fallbackLocal = Vector3.UnitX;

            var fallbackNormal = cylinder.RotateVector(fallbackLocal);

            return new ManifoldPoints(
                closest,
                sphere.Position - fallbackNormal * sphereRadius,
                fallbackNormal,
                sphereRadius);
        }

        var distance = MathF.Sqrt(distanceSq);
        var normal = toSphere / distance;
        var penetrationDepth = sphereRadius - distance;

        return new ManifoldPoints(
            closest,
            sphere.Position - normal * sphereRadius,
            normal,
            penetrationDepth);
    }
}

[ColliderRegister(typeof(CylinderShape), typeof(PlaneShape))]
public sealed class CylinderPlaneCollider : ICollider<CylinderShape, PlaneShape>
{
    private const float Epsilon = 1e-4f;

    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<CylinderShape> cylinder,
        TransformedPhysicShape<PlaneShape> plane)
    {
        var planeNormal = Vector3.Normalize(plane.RotateVector(plane.Shape.Normal));
        var axis = cylinder.GetCylinderAxis();
        var halfHeight = cylinder.GetCylinderHalfHeight();
        var radius = cylinder.GetCylinderRadius();

        var direction = -planeNormal;
        var capSign = Vector3.Dot(axis, direction) >= 0f ? 1f : -1f;
        var capCenter = cylinder.Position + axis * (halfHeight * capSign);

        var perpendicular = direction - axis * Vector3.Dot(direction, axis);
        var perpendicularLength = perpendicular.Length();
        var support = perpendicularLength > Epsilon
            ? capCenter + perpendicular / perpendicularLength * radius
            : capCenter;

        var distance = Vector3.Dot(support - plane.Position, planeNormal) - plane.Shape.Distance;

        if (distance > 0f)
            return ManifoldPoints.Empty;

        var penetrationDepth = -distance;
        var planeContact = support - planeNormal * distance;

        return new ManifoldPoints(support, planeContact, -planeNormal, penetrationDepth);
    }
}

[ColliderRegister(typeof(CylinderShape), typeof(BoxShape))]
public sealed class CylinderBoxCollider : ICollider<CylinderShape, BoxShape>
{
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<CylinderShape> cylinder,
        TransformedPhysicShape<BoxShape> box)
    {
        var cylinderExtents = new Vector3(
            cylinder.GetCylinderRadius(),
            cylinder.GetCylinderHalfHeight(),
            cylinder.GetCylinderRadius());

        return OrientedBoxSat.Collide(
            cylinder.Position,
            cylinder.GetCylinderBoxAxes(),
            cylinderExtents,
            box.Position,
            box.Shape.GetBoxAxes(box),
            box.Shape.GetBoxHalfExtents(box));
    }
}

[ColliderRegister(typeof(CylinderShape), typeof(CylinderShape))]
public sealed class CylinderCylinderCollider : ICollider<CylinderShape, CylinderShape>
{
    public ManifoldPoints ProcessCollision(
        TransformedPhysicShape<CylinderShape> a,
        TransformedPhysicShape<CylinderShape> b)
    {
        var aExtents = new Vector3(
            a.GetCylinderRadius(),
            a.GetCylinderHalfHeight(),
            a.GetCylinderRadius());

        var bExtents = new Vector3(
            b.GetCylinderRadius(),
            b.GetCylinderHalfHeight(),
            b.GetCylinderRadius());

        return OrientedBoxSat.Collide(
            a.Position,
            a.GetCylinderBoxAxes(),
            aExtents,
            b.Position,
            b.GetCylinderBoxAxes(),
            bExtents);
    }
}

internal static class OrientedBoxSat
{
    private const float Epsilon = 1e-4f;
    private const float MaxDepth = 10f;

    public static ManifoldPoints Collide(
        Vector3 aPos, Vector3[] aAxes, Vector3 aExtents,
        Vector3 bPos, Vector3[] bAxes, Vector3 bExtents)
    {
        var ab = bPos - aPos;

        var minOverlap = float.MaxValue;
        var collisionNormal = Vector3.Zero;

        bool TestAxis(Vector3 axis, ref float minOverlap, ref Vector3 collisionNormal)
        {
            if (axis.LengthSquared() < Epsilon * Epsilon)
                return true;

            axis = Vector3.Normalize(axis);

            var aProj = Project(aPos, aAxes, aExtents, axis);
            var bProj = Project(bPos, bAxes, bExtents, axis);

            var overlap = MathF.Min(aProj.Max, bProj.Max) - MathF.Max(aProj.Min, bProj.Min);

            if (overlap < Epsilon)
                return false;

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                collisionNormal = Vector3.Dot(ab, axis) < 0f ? -axis : axis;
            }

            return true;
        }

        for (var i = 0; i < 3; i++)
        {
            if (!TestAxis(aAxes[i], ref minOverlap, ref collisionNormal))
                return ManifoldPoints.Empty;
        }

        for (var i = 0; i < 3; i++)
        {
            if (!TestAxis(bAxes[i], ref minOverlap, ref collisionNormal))
                return ManifoldPoints.Empty;
        }

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                if (!TestAxis(Vector3.Cross(aAxes[i], bAxes[j]), ref minOverlap, ref collisionNormal))
                    return ManifoldPoints.Empty;
            }
        }

        if (minOverlap >= float.MaxValue || minOverlap < Epsilon)
            return ManifoldPoints.Empty;

        var penetrationDepth = MathF.Min(minOverlap, MaxDepth);

        var contactA = Support(aPos, aAxes, aExtents, -collisionNormal);
        var contactB = Support(bPos, bAxes, bExtents, collisionNormal);

        var average = (contactA + contactB) * 0.5f;

        var aContact = average - collisionNormal * (penetrationDepth * 0.5f);
        var bContact = average + collisionNormal * (penetrationDepth * 0.5f);

        return new ManifoldPoints(aContact, bContact, collisionNormal, penetrationDepth);
    }

    private static (float Min, float Max) Project(
        Vector3 center, Vector3[] axes, Vector3 extents, Vector3 axis)
    {
        var centerProjection = Vector3.Dot(center, axis);

        var radius = MathF.Abs(Vector3.Dot(axes[0], axis)) * extents.X +
                     MathF.Abs(Vector3.Dot(axes[1], axis)) * extents.Y +
                     MathF.Abs(Vector3.Dot(axes[2], axis)) * extents.Z;

        return (centerProjection - radius, centerProjection + radius);
    }

    private static Vector3 Support(Vector3 center, Vector3[] axes, Vector3 extents, Vector3 direction)
    {
        var offset = Vector3.Zero;

        offset += axes[0] * (Vector3.Dot(axes[0], direction) >= 0f ? extents.X : -extents.X);
        offset += axes[1] * (Vector3.Dot(axes[1], direction) >= 0f ? extents.Y : -extents.Y);
        offset += axes[2] * (Vector3.Dot(axes[2], direction) >= 0f ? extents.Z : -extents.Z);

        return center + offset;
    }
}
