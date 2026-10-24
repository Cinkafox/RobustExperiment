using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Constraints;

namespace Content.Shared.Physics.Systems;

public sealed class ConstraintSystem : EntitySystem
{
    public void AddConstraint(EntityUid bodyA, EntityUid bodyB, IBodyConstraint constraint)
    {
        if (!HasComp<RigidBodyComponent>(bodyA))
        {
            Log.Error("No rigid body component found for body " + bodyA);
            return;
        }
        
        var bodyAConstr = EnsureComp<ConstraintComponent>(bodyA);
        bodyAConstr.Constraints[bodyB] = constraint;
        Dirty(bodyA, bodyAConstr);
    }

    public void RemoveConstraint(EntityUid bodyA, EntityUid bodyB)
    {
        if(!TryComp<ConstraintComponent>(bodyA, out var constraint))
            return;
        
        constraint.Constraints.Remove(bodyB);

        if (constraint.Constraints.Count == 0)
            RemComp<ConstraintComponent>(bodyA);
        else
            Dirty(bodyA, constraint);
    }

    public bool TryGetConstraint<T>(EntityUid bodyA, EntityUid bodyB,[NotNullWhen(true)] out T? constraint) where T : IBodyConstraint
    {
        constraint = default;
        if (!TryComp<ConstraintComponent>(bodyA, out var constraintComponent) ||
            !constraintComponent.Constraints.TryGetValue(bodyB, out var rawConstraint) ||
            rawConstraint is not T correctConstraint)
            return false;
        
        constraint = correctConstraint;
        return true;
    }
}