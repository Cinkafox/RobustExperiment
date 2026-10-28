using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InputMoverComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EulerAngles RotationMovement = EulerAngles.Zero;
    [ViewVariables, AutoNetworkedField] public Vector3 PositionMovement = Vector3.Zero;
    [ViewVariables, AutoNetworkedField] public bool IsJumping = false;
    [ViewVariables(VVAccess.ReadOnly)] public bool IsMoving => PositionMovement != Vector3.Zero;
}