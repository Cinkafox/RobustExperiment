using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.EntityConstraint;
using Content.Shared.Input;
using Content.Shared.Items;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Constraints;
using Content.Shared.Physics.Systems;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Car;

public sealed partial class CarMovementSystem : EntitySystem
{
    [Dependency] private RigidBodySystem _rigidBodySystem = default!;
    [Dependency] private ConstraintSystem _constraintSystem = default!;
    [Dependency] private CameraSystem _cameraSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.CarMoveForwardAction, new SpeedupWheelCarHandler(1, this))
            .Bind(ContentKeyFunctions.CarMoveBackwardAction, new SpeedupWheelCarHandler(-1 , this))
            .Bind(ContentKeyFunctions.CarMoveLeftAction, new RotateWheelCarHandler(-1f, this))
            .Bind(ContentKeyFunctions.CarMoveRightAction, new RotateWheelCarHandler(1f, this))
            //.Bind(ContentKeyFunctions.CarBreak, new ApplyBreakHandler())
            .Register<CarMovementSystem>();
    }

    [SubscribeLocalEvent]
    private void OnWheelSteeringInit(EntityUid uid, CarWheelSteeringComponent component, ComponentInit args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) return;
        if (!_timing.IsFirstTimePredicted)
            return;
        var carComp = EnsureComp<CarComponent>(constraint.ParentUid);
        carComp.Steering.Add(uid);
        Dirty(constraint.ParentUid, carComp);
    }

    [SubscribeLocalEvent]
    private void OnWheelInit(EntityUid uid, CarWheelComponent component, ComponentInit args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) return;
        if (!_timing.IsFirstTimePredicted)
            return;
        var carComp = EnsureComp<CarComponent>(constraint.ParentUid);
        carComp.Wheels.Add(uid);
        Dirty(constraint.ParentUid, carComp);
    }

    [SubscribeLocalEvent]
    private void OnControllerUse(EntityUid uid, CarRemoteControllerComponent component, ItemUseEvent args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) 
            return;
        
        if (!HasComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy))
        {
            AddComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy).Car = constraint.ParentUid;
            _cameraSystem.AddFollowEntity(args.UsedBy, constraint.ParentUid, new Vector3(6,3,2), new EulerAngles(Angle.FromDegrees(15),Angle.FromDegrees(-100),0), true);
        }
        else
        {
            RemComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy);
            _cameraSystem.RemoveFollowEntity(args.UsedBy, constraint.ParentUid);
        }
    }
    
    [SubscribeLocalEvent]
    private void OnItemThrown(EntityUid uid, CarRemoteControllerComponent component, ItemDropEvent args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) 
            return;
        
        if (HasComp<PlayerActiveCarRemoteControlComponent>(args.DroppedBy))
        {
            RemComp<PlayerActiveCarRemoteControlComponent>(args.DroppedBy);
            _cameraSystem.RemoveFollowEntity(args.DroppedBy, constraint.ParentUid);
        }
    }

    public void RotateWheel(EntityUid wheelUid, EntityUid carUid, float dir)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if(!TryComp<CarWheelSteeringComponent>(wheelUid, out var steering)) 
            return;
        if(!_constraintSystem.TryGetConstraint<HingeConstraint>(carUid, wheelUid, out var constraint)) 
            return;
        
        var rotationMatrix = Matrix4x4.CreateRotationY((float)(steering.SteeringAngle * dir));
            
        constraint.Axis = Vector3.Transform(constraint.Axis, rotationMatrix);
        constraint.AxisB = Vector3.Transform(constraint.AxisB, rotationMatrix);
        _constraintSystem.UpdateConstraint(carUid);
    }

    public void SpeedupWheel(EntityUid wheelUid, EntityUid carUid, float dir)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if(!TryComp<CarWheelComponent>(wheelUid, out var wheel))
            return;

        wheel.Speed += dir;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<CarWheelComponent, RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var carWheelComp, out var rigidBodyComponent))
        {
            _rigidBodySystem.ApplyRelativeTorque(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), carWheelComp.Velocity);
        }
    }
}


public sealed class SpeedupWheelCarHandler(float direction, CarMovementSystem carSystem) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if(!entManager.TryGetComponent<PlayerActiveCarRemoteControlComponent>(session?.AttachedEntity, out var playerActiveCarRemoteControl) ||
           !entManager.TryGetComponent<CarComponent>(playerActiveCarRemoteControl.Car, out var carComp)) 
            return false;
        
        var rotSpeed = direction * (message.State is BoundKeyState.Up ?  1 : -1);
        foreach (var rotatingWheels in carComp.Wheels)
        {
            carSystem.SpeedupWheel(rotatingWheels, playerActiveCarRemoteControl.Car, rotSpeed * 15);
        }

        return false;
    }
}

public sealed class RotateWheelCarHandler(float direction, CarMovementSystem carSystem) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if(!entManager.TryGetComponent<PlayerActiveCarRemoteControlComponent>(session?.AttachedEntity, out var playerActiveCarRemoteControl) ||
           !entManager.TryGetComponent<CarComponent>(playerActiveCarRemoteControl.Car, out var carComp)) 
            return false;
        
        var rotSpeed = direction * (message.State is BoundKeyState.Up ?  1 : -1);
        foreach (var rotatingWheels in carComp.Wheels)
        {
            carSystem.RotateWheel(rotatingWheels, playerActiveCarRemoteControl.Car, rotSpeed);
        }

        return false;
    }
}
