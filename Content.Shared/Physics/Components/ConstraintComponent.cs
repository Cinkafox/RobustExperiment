using Content.Shared.Physics.Constraints;

namespace Content.Shared.Physics.Components;

[RegisterComponent]
public sealed partial class ConstraintComponent : Component
{
    [ViewVariables] public Dictionary<EntityUid, IBodyConstraint> Constraints = [];
}