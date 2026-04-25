using Content.Shared.Transform;
using Robust.Shared.Analyzers;
using IPhysicShape = Content.Shared.Physics.Shapes.IPhysicShape;

namespace Content.Shared.Physics.Data;

[Virtual]
public class TransformedPhysicShape
{
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
    public Quaternion Conjugate => Quaternion.Conjugate(Rotation);
    
    public IPhysicShape Shape { get; }
    public TransformedPhysicShape(Transform3dComponent aTransform, IPhysicShape shape)
    {
        Shape = shape;

        Position = aTransform.WorldPosition;
        Rotation = aTransform.WorldRotation;
        Scale = aTransform.WorldScale;
    }
    
    public TransformedPhysicShape(IPhysicShape shape)
    {
        Shape = shape;
    }
    
    public TransformedPhysicShape(TransformedPhysicShape transformedPhysicShape)
    {
        Shape = transformedPhysicShape.Shape;
        Position = transformedPhysicShape.Position;
        Rotation = transformedPhysicShape.Rotation;
        Scale = transformedPhysicShape.Scale;
    }

    public Vector3 RotateVector(Vector3 vector)
    {
        return Vector3.Transform(vector, Rotation);
    }

    public Vector3 RotateConjugate(Vector3 vector)
    {
        return Vector3.Transform(vector, Conjugate);
    }
}

public sealed class TransformedPhysicShape<T>: TransformedPhysicShape where T : IPhysicShape
{
    public new T Shape { get; }
    
    public TransformedPhysicShape(Transform3dComponent aTransform, T shape) : base(aTransform, shape)
    {
        Shape = shape;

        Position = aTransform.WorldPosition;
        Rotation = aTransform.WorldRotation;
        Scale = aTransform.WorldScale;
    }
    
    public TransformedPhysicShape(T shape) : base(shape)
    {
        Shape = shape;
    }

    public TransformedPhysicShape(TransformedPhysicShape transformedPhysicShape) : base(transformedPhysicShape)
    {
        Shape = (T)transformedPhysicShape.Shape;
    }
}