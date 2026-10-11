using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Constraints;

[ImplicitDataDefinitionForInheritors]
public partial interface IBodyConstraint
{
    public void Prepare(RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent?, Transform3dComponent> bodyB, 
        float dt);
    public void Solve(RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent?, Transform3dComponent> bodyB, 
        float dt);

    public void DrawDebug(DebugDrawingHandle handle);
}