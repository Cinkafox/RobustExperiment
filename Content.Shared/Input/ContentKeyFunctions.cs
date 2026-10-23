using Robust.Shared.Input;

namespace Content.Shared.Input;

[KeyFunctions]
public static class ContentKeyFunctions
{
    public static readonly BoundKeyFunction PlayerJumpAction = "PlayerJumpAction";
    public static readonly BoundKeyFunction PlayerDropItemAction = "PlayerDropItemAction";
    public static readonly BoundKeyFunction PlayerPickupAction = "PlayerPickupAction";
    
    public static readonly BoundKeyFunction CarMoveForwardAction = "CarMoveForwardAction";
    public static readonly BoundKeyFunction CarMoveBackwardAction = "CarMoveBackwardAction";
    public static readonly BoundKeyFunction CarMoveLeftAction = "CarMoveLeftAction";
    public static readonly BoundKeyFunction CarMoveRightAction = "CarMoveRightAction";
    public static readonly BoundKeyFunction CarBreak = "CarBreakAction";
}