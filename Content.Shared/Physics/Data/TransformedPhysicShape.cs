using Content.Shared.Transform;
using Content.Shared.Utils;
using IPhysicShape = Content.Shared.Physics.Shapes.IPhysicShape;

namespace Content.Shared.Physics.Data;

public struct TransformedPhysicShape
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public readonly Quaternion Conjugate => Quaternion.Conjugate(Rotation);

    public IPhysicShape Shape { get; }

    public TransformedPhysicShape(Transform3dComponent aTransform, IPhysicShape shape)
        : this(shape, aTransform.WorldPosition, aTransform.WorldRotation, aTransform.WorldScale)
    {
    }

    public TransformedPhysicShape(IPhysicShape shape, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Shape = shape;
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public TransformedPhysicShape(IPhysicShape shape)
    {
        Shape = shape;
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    public TransformedPhysicShape(TransformedPhysicShape transformedPhysicShape)
    {
        this = transformedPhysicShape;
    }

    public readonly Vector3 RotateVector(Vector3 vector)
    {
        return Matrix4Helpers.TransformVector(vector, Rotation);
    }

    public readonly Vector3 RotateConjugate(Vector3 vector)
    {
        return Matrix4Helpers.TransformVector(vector, Conjugate);
    }
}

public readonly struct TransformedPhysicShape<T> where T : IPhysicShape
{
    private readonly TransformedPhysicShape _base;

    public T Shape { get; }

    public Vector3 Position => _base.Position;
    public Quaternion Rotation => _base.Rotation;
    public Vector3 Scale => _base.Scale;
    public Quaternion Conjugate => _base.Conjugate;

    public TransformedPhysicShape(Transform3dComponent aTransform, T shape)
        : this(new TransformedPhysicShape(aTransform, shape))
    {
    }

    public TransformedPhysicShape(T shape)
        : this(new TransformedPhysicShape(shape))
    {
    }

    public TransformedPhysicShape(TransformedPhysicShape transformedPhysicShape)
    {
        _base = transformedPhysicShape;
        Shape = (T)transformedPhysicShape.Shape;
    }

    public Vector3 RotateVector(Vector3 vector)
    {
        return _base.RotateVector(vector);
    }

    public Vector3 RotateConjugate(Vector3 vector)
    {
        return _base.RotateConjugate(vector);
    }

    public static implicit operator TransformedPhysicShape(TransformedPhysicShape<T> shape)
    {
        return shape._base;
    }
}