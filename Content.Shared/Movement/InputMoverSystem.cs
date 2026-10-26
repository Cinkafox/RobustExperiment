using System.Numerics;
using Content.Shared.Input;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Shared.Movement;

public sealed partial class InputMoverSystem : EntitySystem
{
    [Dependency] private RigidBodySystem _rigidBodySystem = default!;
    
    
    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new ApplyMovementHandler(new Vector3(0, 0, -1)))
            .Bind(EngineKeyFunctions.MoveDown, new ApplyMovementHandler(new Vector3(0, 0, 1)))
            .Bind(EngineKeyFunctions.MoveLeft, new ApplyMovementHandler(new Vector3(-1, 0, 0)))
            .Bind(EngineKeyFunctions.MoveRight, new ApplyMovementHandler(new Vector3(1, 0, 0)))
            .Bind(ContentKeyFunctions.PlayerJumpAction, new ApplyJumpHandler())
            .Register<InputMoverSystem>();
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent, Transform3dComponent, RigidBodyComponent>();

        while (query.MoveNext(out var uid, out var inputMover, out var transform3dComponent, out var rigidBodyComponent))
        {
            if (!_rigidBodySystem.ShouldPredict(uid))
                continue;

            if (rigidBodyComponent.IsGrounded != rigidBodyComponent.IsGrounding)
            {
                RaiseLocalEvent(uid, new EntityGroundStatusChangedEvent(rigidBodyComponent.IsGrounded));
                rigidBodyComponent.IsGrounded = rigidBodyComponent.IsGrounding;
            }
            
            var airFactor = rigidBodyComponent.IsGrounded ? 65f : 5f;
            var normalizedPositionMovement = Vector3.Zero;
            if (inputMover.PositionMovement != Vector3.Zero)
            {
                normalizedPositionMovement = Vector3.Normalize(inputMover.PositionMovement);
            }
            
            var shift = normalizedPositionMovement * airFactor;
            var toYaw = Angle.FromWorldVec(new Vector2(normalizedPositionMovement.X,
                -normalizedPositionMovement.Z));
                
            var delta = transform3dComponent.LocalAngle.Yaw - toYaw;
                
            delta = (delta % (MathF.PI * 2f) + MathF.PI * 3f) % (MathF.PI * 2f) - MathF.PI;
                
            if(normalizedPositionMovement != Vector3.Zero)
                transform3dComponent.LocalAngle = new EulerAngles(transform3dComponent.WorldAngle.Pitch, 
                    transform3dComponent.WorldAngle.Yaw - delta * 0.6, 
                    transform3dComponent.WorldAngle.Roll);
            
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), shift * rigidBodyComponent.Mass * frameTime);
            
            if(!rigidBodyComponent.IsGrounded || !inputMover.IsJumping) 
                continue;
                
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), new Vector3(0, 300, 0) * rigidBodyComponent.Mass * frameTime);
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