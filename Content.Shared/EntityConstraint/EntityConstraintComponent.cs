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

[RegisterComponent]
public sealed partial class EntityConstraintChildComponent : Component
{
    [ViewVariables] public EntityUid ParentUid;
}

[DataDefinition]
public sealed partial class EntityConstraint
{
    [DataField] public EntProtoId Entity;
    [DataField] public Vector3 Offset = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public IBodyConstraint? BodyConstraint;
    [DataField] public ComponentRegistry Components = new();
}