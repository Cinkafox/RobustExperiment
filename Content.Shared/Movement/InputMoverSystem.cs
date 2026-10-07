using System.Numerics;
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

    public const bool TopDownMode = true;
    
    public override void Initialize()
    {
        base.Initialize();
        var builder = CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new ApplyMovementHandler(new Vector3(0, 0, -1)))
            .Bind(EngineKeyFunctions.MoveDown, new ApplyMovementHandler(new Vector3(0, 0, 1)))
            .Bind(ContentKeyFunctions.PlayerJumpAction, new ApplyJumpHandler());

        if (TopDownMode)
        {
            builder
                .Bind(EngineKeyFunctions.MoveLeft, new ApplyMovementHandler(new Vector3(-1, 0, 0)))
                .Bind(EngineKeyFunctions.MoveRight, new ApplyMovementHandler(new Vector3(1, 0, 0)));
        }
        else
        {
            builder
                .Bind(EngineKeyFunctions.MoveLeft,
                    new ApplyRotationMovementHandler(new EulerAngles(0, Angle.FromDegrees(100), 0)))
                .Bind(EngineKeyFunctions.MoveRight,
                    new ApplyRotationMovementHandler(new EulerAngles(0, Angle.FromDegrees(-100), 0)));
        }
        
        builder.Register<InputMoverSystem>();
    }
    
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent, Transform3dComponent, RigidBodyComponent>();

        while (query.MoveNext(out var uid, out var inputMover, out var transform3dComponent, out var rigidBodyComponent))
        {
            if (rigidBodyComponent.IsGrounded != rigidBodyComponent.IsGrounding)
            {
                RaiseLocalEvent(uid, new EntityGroundStatusChangedEvent(rigidBodyComponent.IsGrounded));
                rigidBodyComponent.IsGrounded = rigidBodyComponent.IsGrounding;
            }

            var airFactor = rigidBodyComponent.IsGrounded ? 40f : 5f;
            
            var shift = inputMover.PositionMovement * airFactor;
            if (!TopDownMode)
            {
                transform3dComponent.LocalRotation *= (inputMover.RotationMovement * frameTime).ToQuaternion();
                
                shift = 
                    Vector3.Transform(shift, 
                        Matrix4Helpers.CreateRotationY(transform3dComponent.WorldAngle.Yaw));
            }
            else
            {
                var toYaw = Angle.FromWorldVec(new Vector2(inputMover.PositionMovement.X,
                    -inputMover.PositionMovement.Z));
                
                var delta = transform3dComponent.LocalAngle.Yaw - toYaw;
                
                delta = (delta % (MathF.PI * 2f) + MathF.PI * 3f) % (MathF.PI * 2f) - MathF.PI;
                
                if(inputMover.PositionMovement != Vector3.Zero)
                    transform3dComponent.LocalAngle = new EulerAngles(transform3dComponent.WorldAngle.Pitch, 
                        transform3dComponent.WorldAngle.Yaw - delta * 0.6, 
                        transform3dComponent.WorldAngle.Roll);
            }
            
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), shift * rigidBodyComponent.Mass * frameTime);
            
            if(!rigidBodyComponent.IsGrounded || !inputMover.IsJumping) 
                continue;
                
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), new Vector3(0, 500, 0) * rigidBodyComponent.Mass * frameTime);
            inputMover.IsJumping = false;
            RaiseLocalEvent(uid, new EntityJumpedEvent());
        }
    }
}

public sealed class EntityGroundStatusChangedEvent(bool inGround) : EntityEventArgs
{
    public bool InGround { get; } = inGround;
}

public sealed class EntityJumpedEvent : EntityEventArgs
{
}