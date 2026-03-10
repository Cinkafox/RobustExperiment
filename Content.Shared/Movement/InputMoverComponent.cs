using Content.Shared.Utils;

namespace Content.Shared.Movement;

[RegisterComponent]
public sealed partial class InputMoverComponent : Component
{
    [ViewVariables] public EulerAngles RotationMovement = EulerAngles.Zero;
    [ViewVariables] public Vector3 PositionMovement = Vector3.Zero;
    [ViewVariables] public bool IsJumping = false;
}