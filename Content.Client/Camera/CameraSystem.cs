using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.Camera;

public sealed class CameraSystem : EntitySystem
{
    [Dependency] private readonly CameraManager _cameraManager = default!;
    
    public Vector3 Shifter = Vector3.Zero;
    public EulerAngles AngleShift = new();
    
    public override void Initialize()
    {
        SubscribeLocalEvent<CameraComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<CameraComponent, LocalPlayerDetachedEvent>(OnDeattached);
        
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new GoInputHandler(this,new Vector3(0, 0, -1)))
            .Bind(EngineKeyFunctions.MoveDown, new GoInputHandler(this,new Vector3(0, 0, 1)))
            .Bind(EngineKeyFunctions.MoveLeft, new AngleInputHandler(this,new EulerAngles(0, Angle.FromDegrees(2),0)))
            .Bind(EngineKeyFunctions.MoveRight, new AngleInputHandler(this,new EulerAngles(0, Angle.FromDegrees(-2),0)))
            .Register<CameraSystem>();
    }

    private void OnDeattached(Entity<CameraComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _cameraManager.Camera = null;
    }

    private void OnAttached(Entity<CameraComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if(!TryComp<Transform3dComponent>(ent, out var transform3dComponent)) 
            return;

        _cameraManager.Camera = (transform3dComponent, ent.Comp, ent);
    }

    public override void Update(float frameTime)
    {
        if(!_cameraManager.Camera.HasValue) return;
        var transform = _cameraManager.Camera.Value.Item1;

        transform.LocalAngle -= AngleShift;

        var shift = Vector3.Transform(Shifter, Matrix4Helpers.CreateRotationY(transform.WorldAngle.Yaw));

        transform.LocalPosition += shift * frameTime * 3;
    }
}

public sealed class AngleInputHandler : InputCmdHandler
{
    private readonly CameraSystem _cameraSystem;
    private readonly EulerAngles _to;

    public AngleInputHandler(CameraSystem cameraSystem, EulerAngles to)
    {
        _cameraSystem = cameraSystem;
        _to = to;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (message.State is BoundKeyState.Up)
        {
            _cameraSystem.AngleShift += _to;
        }
        else
        {
            _cameraSystem.AngleShift -= _to;
        }
        
        return false;
    }
}

public sealed class GoInputHandler : InputCmdHandler
{
    private readonly CameraSystem _cameraSystem;
    private readonly Vector3 _to;

    public GoInputHandler(CameraSystem cameraSystem, Vector3 to)
    {
        _cameraSystem = cameraSystem;
        _to = to;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (message.State is BoundKeyState.Up)
        {
            _cameraSystem.Shifter += _to;
        }
        else
        {
            _cameraSystem.Shifter -= _to;
        }
        
        return false;
    }
}