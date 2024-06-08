using Robust.Shared.GameStates;

namespace Content.Shared.Movement;

[RegisterComponent, NetworkedComponent]
public sealed partial class InputMoverComponent : Component
{
    [ViewVariables] public MoveButtons PushedButtons;
}