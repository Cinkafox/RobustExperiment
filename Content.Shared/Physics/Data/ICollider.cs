using Content.Shared.Physics.Components;
using IPhysicShape = Content.Shared.Physics.Shapes.IPhysicShape;

namespace Content.Shared.Physics.Data;

public interface ICollider
{
    public ManifoldPoints ProcessCollision(TransformedPhysicShape a, TransformedPhysicShape b);
}

public interface ICollider<T1, T2>: ICollider where T1 : IPhysicShape where T2: IPhysicShape
{
    public ManifoldPoints ProcessCollision(TransformedPhysicShape<T1> a, TransformedPhysicShape<T2> b);

    ManifoldPoints ICollider.ProcessCollision(TransformedPhysicShape a, TransformedPhysicShape b)
    {
        return ProcessCollision(new TransformedPhysicShape<T1>(a),new TransformedPhysicShape<T2>(b));
    }
}
