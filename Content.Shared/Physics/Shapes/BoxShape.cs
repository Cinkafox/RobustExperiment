using Content.Shared.Physics.Data;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics.Shapes;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BoxShape : IPhysicShape
{
    [DataField] public Vector3 HalfExtents = new(0.5f, 0.5f, 0.5f);
    [ViewVariables] public Vector3 Size => HalfExtents * 2f;

    public float Area => HalfExtents.X * 2f * HalfExtents.Y * 2f * HalfExtents.Z * 2f;

    public Vector3 CalculateLocalInertia(float mass)
    {
        var x = 2f * HalfExtents.X;
        var y = 2f * HalfExtents.Y;
        var z = 2f * HalfExtents.Z;

        var ixx = (mass / 12f) * (y * y + z * z);
        var iyy = (mass / 12f) * (x * x + z * z);
        var izz = (mass / 12f) * (x * x + y * y);

        return new Vector3(ixx, iyy, izz);
    }

    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape)
    {
        var vertices = GetBoxVertices(transformedPhysicShape);
        handle.DrawVertex([
            vertices[0],
            vertices[1],
            vertices[2],
            vertices[3]
        ]);

        handle.DrawVertex([
            vertices[4],
            vertices[5],
            vertices[6],
            vertices[7]
        ]);

        handle.DrawVertex([
            vertices[0],
            vertices[1],
            vertices[4],
            vertices[5]
        ]);

        handle.DrawVertex([
            vertices[3],
            vertices[2],
            vertices[6],
            vertices[7]
        ]);

        handle.DrawVertex([
            vertices[0],
            vertices[3],
            vertices[4],
            vertices[7]
        ]);

        handle.DrawVertex([
            vertices[1],
            vertices[2],
            vertices[5],
            vertices[6]
        ]);
    }

    public Vector3[] GetBoxVertices(TransformedPhysicShape transformedPhysicShape)
    {
        var worldVertices = new Vector3[8];
        GetBoxVertices(transformedPhysicShape, worldVertices);
        return worldVertices;
    }

    public void GetBoxVertices(TransformedPhysicShape transformedPhysicShape, Span<Vector3> destination)
    {
        if (destination.Length < 8)
            throw new ArgumentException("Destination must have at least 8 elements", nameof(destination));

        // Apply non-uniform scale to half-extents
        var scaledExtents = new Vector3(
            MathF.Abs(transformedPhysicShape.Scale.X) * HalfExtents.X,
            MathF.Abs(transformedPhysicShape.Scale.Y) * HalfExtents.Y,
            MathF.Abs(transformedPhysicShape.Scale.Z) * HalfExtents.Z
        );

        for (var i = 0; i < 8; i++)
        {
            var local = new Vector3(
                (i & 1) == 0 ? -scaledExtents.X : scaledExtents.X,
                (i & 2) == 0 ? -scaledExtents.Y : scaledExtents.Y,
                (i & 4) == 0 ? -scaledExtents.Z : scaledExtents.Z);

            destination[i] = transformedPhysicShape.Position + transformedPhysicShape.RotateVector(local);
        }
    }

    public BoxAxes GetBoxAxes(TransformedPhysicShape transformedPhysicShape)
    {
        return new BoxAxes(
            Vector3.Normalize(transformedPhysicShape.RotateVector(new Vector3(1, 0, 0))),
            Vector3.Normalize(transformedPhysicShape.RotateVector(new Vector3(0, 1, 0))),
            Vector3.Normalize(transformedPhysicShape.RotateVector(new Vector3(0, 0, 1))));
    }

    public Vector3 GetBoxHalfExtents(TransformedPhysicShape transformedPhysicShape)
    {
        return new Vector3(
            MathF.Abs(transformedPhysicShape.Scale.X) * HalfExtents.X,
            MathF.Abs(transformedPhysicShape.Scale.Y) * HalfExtents.Y,
            MathF.Abs(transformedPhysicShape.Scale.Z) * HalfExtents.Z
        );
    }

    public Vector3 GetClosestPointOnBox(TransformedPhysicShape transformedPhysicShape, Vector3 point)
    {
        // Transform point to box-local space

        var localPoint = transformedPhysicShape.RotateConjugate(point - transformedPhysicShape.Position);
        var scaledExtents = GetBoxHalfExtents(transformedPhysicShape);

        // Clamp to box surface in local space
        var localClosest = new Vector3(
            MathHelper.Clamp(localPoint.X, -scaledExtents.X, scaledExtents.X),
            MathHelper.Clamp(localPoint.Y, -scaledExtents.Y, scaledExtents.Y),
            MathHelper.Clamp(localPoint.Z, -scaledExtents.Z, scaledExtents.Z)
        );

        // Transform back to world space
        return transformedPhysicShape.Position + transformedPhysicShape.RotateVector(localClosest);
    }
}