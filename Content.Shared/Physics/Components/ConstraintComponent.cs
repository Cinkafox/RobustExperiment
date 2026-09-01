using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Components;

[RegisterComponent]
public sealed partial class ConstraintComponent : Component
{
    [DataField(required: true)] public IBodyConstraint Constraint;
    [ViewVariables] public EntityUid ConstraintUid;
}