using System.Numerics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement;

public partial class SharedMoverController
{
    public void InitializeInput()
    {
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MoverDirInputCmdHandler(this, MoveButtons.Up))
            .Bind(EngineKeyFunctions.MoveLeft, new MoverDirInputCmdHandler(this, MoveButtons.Left))
            .Bind(EngineKeyFunctions.MoveRight, new MoverDirInputCmdHandler(this, MoveButtons.Right))
            .Bind(EngineKeyFunctions.MoveDown, new MoverDirInputCmdHandler(this, MoveButtons.Down))
            .Register<SharedMoverController>();
    }
    
    public void HandleDirChange(EntityUid entity, MoveButtons buttons, ushort subTick, bool state)
    {
        if(!MoverQuery.TryComp(entity, out var inputMoverComponent)) 
            return;

        if (state)
            inputMoverComponent.PushedButtons |= buttons;
        else
            inputMoverComponent.PushedButtons &= ~buttons;
        
        Logger.Debug(inputMoverComponent.PushedButtons.ToDir() + "??" + ToPrettyString(entity));
    }
}

sealed class MoverDirInputCmdHandler : InputCmdHandler
{
    private readonly SharedMoverController _controller;
    private readonly MoveButtons _buttons;

    public MoverDirInputCmdHandler(SharedMoverController controller, MoveButtons buttons)
    {
        _controller = controller;
        _buttons = buttons;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null) return false;

        _controller.HandleDirChange(session.AttachedEntity.Value, _buttons, message.SubTick, message.State == BoundKeyState.Down);
        return false;
    }
}

[Flags]
[Serializable, NetSerializable]
public enum MoveButtons : byte
{
    None = 0,
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Walk = 16,
    AnyDirection = Up | Down | Left | Right,
}

public static class ShitExt
{
    public static Direction ToDir(this MoveButtons moveButtons)
    {
        return moveButtons switch
        {
            MoveButtons.Up => Direction.North,
            MoveButtons.Down => Direction.South,
            MoveButtons.Left => Direction.West,
            MoveButtons.Right => Direction.East,
            MoveButtons.Up | MoveButtons.Left => Direction.NorthWest,
            MoveButtons.Up | MoveButtons.Right => Direction.NorthEast,
            MoveButtons.Down | MoveButtons.Left => Direction.SouthWest,
            MoveButtons.Down | MoveButtons.Right => Direction.SouthEast,
            _ => Direction.Invalid
        };
    }
}