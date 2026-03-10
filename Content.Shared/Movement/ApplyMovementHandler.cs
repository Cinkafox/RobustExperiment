using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

public sealed class ApplyMovementHandler(Vector3 to) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (!entManager.TryGetComponent<InputMoverComponent>(session?.AttachedEntity, out var inputMover)) 
            return true;
        
        if (message.State is BoundKeyState.Up)
            inputMover.PositionMovement += to;
        else
            inputMover.PositionMovement -= to;
        
        return true;
    }
}