using Content.Shared.Physics.Data;
using Content.Shared.Utils;

namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class BoxShape : IPhysicShape
{
    [DataField] public Vector3 HalfExtents = new(0.5f, 0.5f, 0.5f);
    public float Area => HalfExtents.X * 2f * HalfExtents.Y * 2f * HalfExtents.Z * 2f;
    
    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape)
    {
        var vertices = GetBoxVertices(transformedPhysicShape);
        handle.DrawVertex(new []
        {
            vertices[0],
            vertices[1],
            vertices[2],
            vertices[3],
            
            vertices[4],
            vertices[5],
            vertices[6],
            vertices[7],
        });
    }

    public Vector3[] GetBoxVertices(TransformedPhysicShape transformedPhysicShape)
    {
        // Apply non-uniform scale to half-extents
        var scaledExtents = new Vector3(
            MathF.Abs(transformedPhysicShape.Scale.X) * HalfExtents.X,
            MathF.Abs(transformedPhysicShape.Scale.Y) * HalfExtents.Y,
            MathF.Abs(transformedPhysicShape.Scale.Z) * HalfExtents.Z
        );
        
        // Local-space vertices (±extents on each axis)
        var localVertices = new[]
        {
            new Vector3(-scaledExtents.X, -scaledExtents.Y, -scaledExtents.Z),
            new Vector3( scaledExtents.X, -scaledExtents.Y, -scaledExtents.Z),
            new Vector3( scaledExtents.X,  scaledExtents.Y, -scaledExtents.Z),
            new Vector3(-scaledExtents.X,  scaledExtents.Y, -scaledExtents.Z),
            new Vector3(-scaledExtents.X, -scaledExtents.Y,  scaledExtents.Z),
            new Vector3( scaledExtents.X, -scaledExtents.Y,  scaledExtents.Z),
            new Vector3( scaledExtents.X,  scaledExtents.Y,  scaledExtents.Z),
            new Vector3(-scaledExtents.X,  scaledExtents.Y,  scaledExtents.Z)
        };
        
        // Transform to world space
        var worldVertices = new Vector3[8];
        for (var i = 0; i < 8; i++)
            worldVertices[i] = transformedPhysicShape.Position + transformedPhysicShape.Rotation.RotateVec(localVertices[i]);
        
        return worldVertices;
    }
    
    public Vector3[] GetBoxAxes(TransformedPhysicShape transformedPhysicShape)
    {
        return new[]
        {
            Vector3.Normalize(transformedPhysicShape.Rotation.RotateVec(new Vector3(1, 0, 0))),
            Vector3.Normalize(transformedPhysicShape.Rotation.RotateVec(new Vector3(0, 1, 0))),
            Vector3.Normalize(transformedPhysicShape.Rotation.RotateVec(new Vector3(0, 0, 1)))
        };
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
        var localPoint =transformedPhysicShape.Rotation.Conjugate.RotateVec(point - transformedPhysicShape.Position);
        var scaledExtents = GetBoxHalfExtents(transformedPhysicShape);
        
        // Clamp to box surface in local space
        var localClosest = new Vector3(
            MathHelper.Clamp(localPoint.X, -scaledExtents.X, scaledExtents.X),
            MathHelper.Clamp(localPoint.Y, -scaledExtents.Y, scaledExtents.Y),
            MathHelper.Clamp(localPoint.Z, -scaledExtents.Z, scaledExtents.Z)
        );
        
        // Transform back to world space
        return transformedPhysicShape.Position + transformedPhysicShape.Rotation.RotateVec(localClosest);
    }
}