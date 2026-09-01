using Content.Shared.Physics.Components;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public partial class RigidBodySystem
{
    private void PrepareConstraints(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent, ConstraintComponent>();
        while (query.MoveNext(out var uid, out var body, out var transform, out var constraint))
        {
            if (body.PhysType == PhysType.Static) continue;

            var bodyA = new Entity<RigidBodyComponent, Transform3dComponent>(uid, body, transform);

            var bUid = constraint.ConstraintUid;
            
            if (!bUid.Valid)
            {
                constraint.Constraint.Prepare(this, bodyA, null, deltaTime);
            }
            else if(TryComp<RigidBodyComponent>(bUid, out var rigidBodyB) && 
                    TryComp<Transform3dComponent>(bUid, out var transformB))
            {
                var bodyB = new Entity<RigidBodyComponent, Transform3dComponent>(bUid, rigidBodyB, transformB);
                constraint.Constraint.Prepare(this, bodyA, bodyB, deltaTime);
            }
            else
            {
                Logger.Error($"Constraint not found for {uid}");
            }
        }
    }

    private void SolveConstraints(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent, ConstraintComponent>();
        while (query.MoveNext(out var uid, out var body, out var transform, out var constraint))
        {
            if (body.PhysType == PhysType.Static) continue;

            var bodyA = new Entity<RigidBodyComponent, Transform3dComponent>(uid, body, transform);

            var bUid = constraint.ConstraintUid;
            
            if (!bUid.Valid)
            {
                constraint.Constraint.Solve(this, bodyA, null, deltaTime);
            }
            else if(TryComp<RigidBodyComponent>(bUid, out var rigidBodyB) && 
                    TryComp<Transform3dComponent>(bUid, out var transformB))
            {
                var bodyB = new Entity<RigidBodyComponent, Transform3dComponent>(bUid, rigidBodyB, transformB);
                constraint.Constraint.Solve(this, bodyA, bodyB, deltaTime);
            }
            else
            {
                Logger.Error($"Constraint not found for {uid}");
            }
        }
    }
    
}