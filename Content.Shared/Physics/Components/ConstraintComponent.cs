using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Components;

[RegisterComponent]
public sealed partial class ConstraintComponent : Component
{
    [ViewVariables] public Dictionary<EntityUid, IBodyConstraint> Constraints = [];
}