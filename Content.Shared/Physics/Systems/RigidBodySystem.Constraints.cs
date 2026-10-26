using Content.Shared.Physics.Components;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public partial class RigidBodySystem
{
    private void PrepareConstraints(float deltaTime)
    {
        BaseConstraintQuery(ConstraintPhase.Prepare, deltaTime);
    }

    private void SolveConstraints(float deltaTime)
    {
        BaseConstraintQuery(ConstraintPhase.Solve, deltaTime);
    }

    private enum ConstraintPhase
    {
        Prepare,
        Solve
    }

    private void BaseConstraintQuery(ConstraintPhase phase, float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent, ConstraintComponent>();
        while (query.MoveNext(out var uid, out var body, out var transform, out var constraint))
        {
            var bodyA = new Entity<RigidBodyComponent, Transform3dComponent>(uid, body, transform);

            foreach (var (bodyBUid, bodyConstraint) in constraint.Constraints)
            {
                TryComp<RigidBodyComponent>(bodyBUid, out var rigidBodyB);

                if (_netManager.IsClient)
                {
                    if (body.PhysType == PhysType.Dynamic && !ShouldPredict(uid))
                        continue;

                    if (rigidBodyB is { PhysType: PhysType.Dynamic } && !ShouldPredict(bodyBUid))
                        continue;
                }
                
                var bodyB = new Entity<RigidBodyComponent?, Transform3dComponent>(bodyBUid, rigidBodyB, Comp<Transform3dComponent>(bodyBUid));

                if (phase == ConstraintPhase.Prepare)
                    bodyConstraint.Prepare(this, bodyA, bodyB, deltaTime);
                else
                    bodyConstraint.Solve(this, bodyA, bodyB, deltaTime);
            }
        }
    }
    
}