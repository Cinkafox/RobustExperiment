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
        
        Vector3 ordinalPos;

        if (message.State is BoundKeyState.Up)
            ordinalPos = to;
        else
            ordinalPos = -to;
        
        var ev = new EntityMovingEvent(ordinalPos);
        entManager.EventBus.RaiseLocalEvent(session.AttachedEntity.Value, ev);
        
        if (ev.Handled)
            return true;
        
        SetEntityMoving(entManager, session.AttachedEntity.Value, inputMover, ordinalPos);
        return true;
    }

    private void SetEntityMoving(IEntityManager entManager, EntityUid attachedUid, InputMoverComponent inputMover, Vector3 ordinalPos)
    {
        var oldPos = inputMover.PositionMovement;
        inputMover.PositionMovement += ordinalPos;

        if (oldPos != inputMover.PositionMovement)
        {
            if (inputMover.PositionMovement.Equals(Vector3.Zero))
                entManager.EventBus.RaiseLocalEvent(attachedUid, new EntityEndMoveEvent());
            else
                entManager.EventBus.RaiseLocalEvent(attachedUid, new EntityStartMoveEvent());
        }
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