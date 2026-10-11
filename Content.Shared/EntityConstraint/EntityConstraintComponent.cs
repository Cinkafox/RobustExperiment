using Content.Shared.Physics.Constraints;
using Content.Shared.Utils;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConstraint;

[RegisterComponent]
public sealed partial class EntityConstraintComponent : Component
{
    [DataField] public List<EntityConstraint> Constraints = [];
    [ViewVariables] public List<EntityUid> ActiveConstraints = [];
}

[DataDefinition]
public sealed partial class EntityConstraint
{
    [DataField] public EntProtoId Entity;
    [DataField] public Vector3 Offset = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public IBodyConstraint BodyConstraint = default!;
}