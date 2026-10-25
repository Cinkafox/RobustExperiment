using Content.Shared.Physics.Systems;
using Content.Shared.Transform;

namespace Content.Shared.EntityConstraint;

public sealed partial class EntityConstraintSystem : EntitySystem
{
    [Dependency] private ConstraintSystem _constraintSystem = default!;
    [Dependency] private Transform3dSystem _transform3DSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<EntityConstraintComponent, OnEntityAttachedEvent>(OnAttached);
        SubscribeLocalEvent<EntityConstraintComponent, OnEntityAttachingEvent>(OnAttaching);
    }

    private void OnAttaching(EntityUid uid, EntityConstraintComponent component, OnEntityAttachingEvent args)
    {
        foreach (var constraint in component.ActiveConstraints)
        {
            _constraintSystem.RemoveConstraint(uid, constraint);
            Del(constraint);
        }
        component.ActiveConstraints.Clear();
    }

    private void OnAttached(EntityUid uid, EntityConstraintComponent component, OnEntityAttachedEvent args)
    {
        var mainTransform = Comp<Transform3dComponent>(uid);
        foreach (var constraint in component.Constraints)
        {
            var constrEnt = Spawn(constraint.Entity);
            var constrTrans = Comp<Transform3dComponent>(constrEnt);
            var constrParentComp = AddComp<EntityConstraintChildComponent>(constrEnt);
            constrParentComp.ParentUid = uid;
            
            EntityManager.AddComponents(constrEnt, constraint.Components);
            
            _transform3DSystem.SetParent(constrEnt, args.To);
            constrTrans.LocalPosition = mainTransform.LocalPosition + constraint.Offset;
            constrTrans.LocalAngle = constraint.Rotation;

            if (constraint.BodyConstraint != null)
                _constraintSystem.AddConstraint(uid, constrEnt, constraint.BodyConstraint);
            component.ActiveConstraints.Add(constrEnt);
            
            Dirty(constrEnt, constrParentComp);
        }
        Dirty(uid, component);
    }
}