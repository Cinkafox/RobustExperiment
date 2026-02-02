using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Camera;

public sealed class CameraSystem : EntitySystem
{
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new GoInputHandler(new Vector3(0, 0, -4)))
            .Bind(EngineKeyFunctions.MoveDown, new GoInputHandler(new Vector3(0, 0, 4)))
            .Bind(EngineKeyFunctions.MoveLeft, new AngleInputHandler(new EulerAngles(0, Angle.FromDegrees(-100),0)))
            .Bind(EngineKeyFunctions.MoveRight, new AngleInputHandler(new EulerAngles(0, Angle.FromDegrees(100),0)))
            .Register<CameraSystem>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<CameraComponent, Transform3dComponent>();

        while (query.MoveNext(out var cameraComponent, out var transform3dComponent))
        {
            transform3dComponent.LocalAngle -= cameraComponent.AngleAcceleration * frameTime;

            var shift = 
                Vector3.Transform(cameraComponent.CameraAcceleration, 
                    Matrix4Helpers.CreateRotationY(transform3dComponent.WorldAngle.Yaw));

            transform3dComponent.LocalPosition += shift * frameTime;
        }
    }
}

public sealed class AngleInputHandler : InputCmdHandler
{
   
    private readonly EulerAngles _to;

    public AngleInputHandler(EulerAngles to)
    {
        _to = to;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (!entManager.TryGetComponent<CameraComponent>(session?.AttachedEntity, out var camera)) 
            return true;
        
        if (message.State is BoundKeyState.Up)
            camera.AngleAcceleration += _to;
        else
            camera.AngleAcceleration -= _to;
        
        return true;
    }
}

public sealed class GoInputHandler : InputCmdHandler
{
    private readonly Vector3 _to;

    public GoInputHandler(Vector3 to)
    {
        _to = to;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (!entManager.TryGetComponent<CameraComponent>(session?.AttachedEntity, out var camera)) 
            return true;
        
        if (message.State is BoundKeyState.Up)
            camera.CameraAcceleration += _to;
        else
            camera.CameraAcceleration -= _to;
        
        return true;
    }
}