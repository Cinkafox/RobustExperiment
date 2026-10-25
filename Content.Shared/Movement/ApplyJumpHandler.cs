using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

public sealed class ApplyJumpHandler : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (!entManager.TryGetComponent<InputMoverComponent>(session?.AttachedEntity, out var inputMover)) 
            return false;

        inputMover.IsJumping = message.State is BoundKeyState.Down;
        
        return false;
    }
}