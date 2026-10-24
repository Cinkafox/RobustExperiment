using Content.Shared.Physics.Constraints;
using Robust.Shared.GameStates;

namespace Content.Shared.Physics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConstraintComponent : Component
{
    [ViewVariables, AutoNetworkedField] public Dictionary<EntityUid, IBodyConstraint> Constraints = [];
}