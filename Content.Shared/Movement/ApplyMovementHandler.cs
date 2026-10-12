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
        
        var ordinalPos = Vector3.Zero;

        if (message.State is BoundKeyState.Up)
            ordinalPos = to;
        else
            ordinalPos = -to;
        
        var ev = new EntityMovingEvent(ordinalPos);
        entManager.EventBus.RaiseLocalEvent(session.AttachedEntity.Value, ev);
        if (ev.Handled)
            return true;
        
        var oldPos = inputMover.PositionMovement;
        inputMover.PositionMovement += ordinalPos;

        if (oldPos != inputMover.PositionMovement)
        {
            if (inputMover.PositionMovement.Equals(Vector3.Zero))
                entManager.EventBus.RaiseLocalEvent(session.AttachedEntity.Value, new EntityEndMoveEvent());
            else
                entManager.EventBus.RaiseLocalEvent(session.AttachedEntity.Value, new EntityStartMoveEvent());
        }
        
        return true;
    }
}


public sealed class EntityStartMoveEvent : EntityEventArgs
{
    
}

public sealed class EntityEndMoveEvent : EntityEventArgs
{
    
}

public sealed class EntityMovingEvent(Vector3 positionMovement) : HandledEntityEventArgs
{
    public Vector3 PositionMovement = positionMovement;
}