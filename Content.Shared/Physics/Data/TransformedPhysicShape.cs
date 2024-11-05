using Content.Shared.Physics.Components;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Analyzers;

namespace Content.Shared.Physics.Data;

[Virtual]
public class TransformedPhysicShape
{
    public Vector3 Position = Vector3.Zero;
    public EulerAngles Rotation = EulerAngles.Zero;
    public Vector3 Scale = Vector3.One;
    
    public IPhysicShape Shape { get; }
    public TransformedPhysicShape(Transform3dComponent aTransform, IPhysicShape shape)
    {
        Shape = shape;

        Position = aTransform.LocalPosition;
        Rotation = aTransform.LocalRotation.ToEulerAngle();
        Scale = aTransform.LocalScale;
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
}
public sealed class TransformedPhysicShape<T>: TransformedPhysicShape where T : IPhysicShape
{
    public new T Shape { get; }
    
    public TransformedPhysicShape(Transform3dComponent aTransform, T shape) : base(aTransform, shape)
    {
        Shape = shape;

        Position = aTransform.LocalPosition;
        Rotation = aTransform.LocalRotation.ToEulerAngle();
        Scale = aTransform.LocalScale;
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