using Content.Shared.Movement;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;

namespace Content.Shared.Camera;

public sealed class CameraRotationController : VirtualController
{
    [Dependency] private readonly SharedEyeSystem _sharedEyeSystem = default!;
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.CameraRotateLeft,new RotateCameraInputCmdHandler(CameraRotation.Left))
            .Bind(EngineKeyFunctions.CameraRotateRight,new RotateCameraInputCmdHandler(CameraRotation.Right))
            .Register<CameraRotationController>();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CameraRotationComponent,EyeComponent>();
        while (query.MoveNext(out var uid, out var cameraRotationComponent,out var eyeComponent))
        {
            switch (cameraRotationComponent.CurrentRotation)
            {
                case CameraRotation.None:
                    break;
                case CameraRotation.Left:
                    _sharedEyeSystem.SetRotation(uid,eyeComponent.Rotation + 0.25*frameTime,eyeComponent);
                    Dirty(uid,eyeComponent);
                    break;
                case CameraRotation.Right:
                    _sharedEyeSystem.SetRotation(uid,eyeComponent.Rotation - 0.25*frameTime,eyeComponent);
                    Dirty(uid,eyeComponent);
                    break;
            }
        }
    }
}

sealed class RotateCameraInputCmdHandler : InputCmdHandler
{
    private readonly CameraRotation _rotation;
    public RotateCameraInputCmdHandler(CameraRotation rotation)
    {
        _rotation = rotation;
    }
    
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (session?.AttachedEntity == null) return false;
        var cameraRotationComponent = entManager.EnsureComponent<CameraRotationComponent>(session.AttachedEntity.Value);
        cameraRotationComponent.CurrentRotation = 
            message.State != BoundKeyState.Down ? CameraRotation.None : _rotation;
        return false;
    }
}

