using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

public sealed class ApplyRotationMovementHandler(EulerAngles to) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (!entManager.TryGetComponent<InputMoverComponent>(session?.AttachedEntity, out var inputMover)) 
            return true;
        
        if (message.State is BoundKeyState.Up)
            inputMover.RotationMovement += to;
        else
            inputMover.RotationMovement -= to;
        
        return true;
    }
}