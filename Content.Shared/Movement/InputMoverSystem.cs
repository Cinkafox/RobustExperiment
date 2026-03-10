using Content.Shared.Input;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Shared.Movement;

public sealed class InputMoverSystem : EntitySystem
{
    [Dependency] private readonly RigidBodySystem _rigidBodySystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new ApplyMovementHandler(new Vector3(0, 0, -2)))
            .Bind(EngineKeyFunctions.MoveDown, new ApplyMovementHandler(new Vector3(0, 0, 2)))
            .Bind(EngineKeyFunctions.MoveLeft, new ApplyRotationMovementHandler(new EulerAngles(0, Angle.FromDegrees(-100),0)))
            .Bind(EngineKeyFunctions.MoveRight, new ApplyRotationMovementHandler(new EulerAngles(0, Angle.FromDegrees(100),0)))
            .Bind(ContentKeyFunctions.PlayerJumpAction, new ApplyJumpHandler())
            .Register<InputMoverSystem>();
    }
    
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent, Transform3dComponent, RigidBodyComponent>();

        while (query.MoveNext(out var uid, out var inputMover, out var transform3dComponent, out var rigidBodyComponent))
        {
            transform3dComponent.LocalAngle -= inputMover.RotationMovement * frameTime;

            var shift = 
                Vector3.Transform(inputMover.PositionMovement, 
                    Matrix4Helpers.CreateRotationY(transform3dComponent.WorldAngle.Yaw));
            
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), shift * rigidBodyComponent.Mass);
            
            if (inputMover.IsJumping)
            {
                Log.Debug(rigidBodyComponent.ResolvingPoints.HasContact.ToString());
                if(!rigidBodyComponent.ResolvingPoints.HasContact) return;
                _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), new Vector3(0, 2, 0) * rigidBodyComponent.Mass);
            }
        }
    }
}