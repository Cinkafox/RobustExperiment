using System.Numerics;
using Content.Shared.Mouse;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.Mouse;

public sealed partial class PlayerMouseInputSystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    private Vector2 _lastMousePos = Vector2.Zero;
    private bool _isFirstDelta = true;
    
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        return;
        if(!_clyde.IsFocused)
            return;
        
        var mousePos = _inputManager.MouseScreenPosition.Position;
        
        if(_lastMousePos.EqualsApprox(mousePos) && !_isFirstDelta) 
            return;
        
        _isFirstDelta =! _isFirstDelta;
        var delta = (mousePos - _lastMousePos) * 0.005f;
        
        if(_playerManager.LocalEntity is not null)
            RaiseLocalEvent(_playerManager.LocalEntity.Value, new OnMouseMoveEvent(delta));
        _lastMousePos = mousePos;
    }
}