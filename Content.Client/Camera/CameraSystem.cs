using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Camera;

public sealed class CameraSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    
    public override void Update(float frameTime)
    {
        if(_playerManager.LocalEntity is null || !TryComp<TransformComponent>(_playerManager.LocalEntity.Value,out var transformComponent)) return;

        _eyeManager.CurrentEye.Rotation = transformComponent.WorldRotation;
    }
}