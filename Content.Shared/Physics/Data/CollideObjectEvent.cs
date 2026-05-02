using Content.Shared.Physics.Components;

namespace Content.Shared.Physics.Data;

public interface IBasePhysicsEvent
{
    public Entity<RigidBodyComponent> CollidedEntity { get; }
}

public sealed class CollideObjectEvent(Entity<RigidBodyComponent> collidedEntity)
    : CancellableEntityEventArgs, IBasePhysicsEvent
{
    public Entity<RigidBodyComponent> CollidedEntity { get; } = collidedEntity;
}