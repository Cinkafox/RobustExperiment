using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public partial class RigidBodySystem
{
    private void PrepareConstraints(float deltaTime)
    {
        BaseConstraintQuery(((constraint, a, b) => constraint.Prepare(this, a, b, deltaTime)));
    }

    private void SolveConstraints(float deltaTime)
    {
        BaseConstraintQuery(((constraint, a, b) => constraint.Solve(this, a, b, deltaTime)));
    }
    
    private void BaseConstraintQuery(Action<IBodyConstraint,Entity<RigidBodyComponent, Transform3dComponent>,
        Entity<RigidBodyComponent?, Transform3dComponent>> action)
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent, ConstraintComponent>();
        while (query.MoveNext(out var uid, out var body, out var transform, out var constraint))
        {
            var bodyA = new Entity<RigidBodyComponent, Transform3dComponent>(uid, body, transform);

            foreach (var (bodyBUid, bodyConstraint) in constraint.Constraints)
            {
                TryComp<RigidBodyComponent>(bodyBUid, out var rigidBodyB);
                
                var bodyB = new Entity<RigidBodyComponent?, Transform3dComponent>(bodyBUid, rigidBodyB, Comp<Transform3dComponent>(bodyBUid));
                action(bodyConstraint, bodyA, bodyB);
            }
        }
    }
    
}