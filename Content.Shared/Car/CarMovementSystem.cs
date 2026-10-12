using System.Numerics;
using Content.Shared.EntityConstraint;
using Content.Shared.Items;
using Content.Shared.Movement;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Constraints;
using Content.Shared.Physics.Systems;

namespace Content.Shared.Car;

public sealed partial class CarMovementSystem : EntitySystem
{
    [Dependency] private RigidBodySystem _rigidBodySystem = default!;
    [Dependency] private ConstraintSystem _constraintSystem = default!;

    [SubscribeLocalEvent]
    private void OnWheelSteeringInit(EntityUid uid, CarWheelSteeringComponent component, ComponentInit args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) return;
        var carComp = EnsureComp<CarComponent>(constraint.ParentUid);
        carComp.Steering.Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnWheelInit(EntityUid uid, CarWheelComponent component, ComponentInit args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) return;
        var carComp = EnsureComp<CarComponent>(constraint.ParentUid);
        carComp.Wheels.Add(uid);
    }

    [SubscribeLocalEvent]
    private void OnControllerUse(EntityUid uid, CarRemoteControllerComponent component, ItemUseEvent args)
    {
        if(!TryComp<EntityConstraintChildComponent>(uid, out var constraint)) return;
        if (!HasComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy))
            AddComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy).Car = constraint.ParentUid;
        else
            RemComp<PlayerActiveCarRemoteControlComponent>(args.UsedBy);
    }
    
    [SubscribeLocalEvent]
    private void OnPlayerCarControl(EntityUid uid, PlayerActiveCarRemoteControlComponent component,
        EntityMovingEvent args)
    {
        if(!TryComp<CarComponent>(component.Car, out var carComp)) 
            return;
        
        Log.Debug(args.PositionMovement + " MOVEMENT " + component.Car);

        if (args.PositionMovement.X != 0)
        {
            foreach (var steeringWheel in carComp.Steering)
            {
                RotateWheel(steeringWheel, component.Car, args.PositionMovement.X);
            }
        }
        
        if (args.PositionMovement.Z != 0)
        {
            foreach (var rotatingWheels in carComp.Wheels)
            {
               SpeedupWheel(rotatingWheels, component.Car, args.PositionMovement.Z * 15);
            }
        }
        
        args.Handled = true;
    }

    private void RotateWheel(EntityUid wheelUid, EntityUid carUid, float dir)
    {
        if(!TryComp<CarWheelSteeringComponent>(wheelUid, out var steering)) 
            return;
        if(!_constraintSystem.TryGetConstraint<HingeConstraint>(carUid, wheelUid, out var constraint)) 
            return;
            
        var rotationMatrix = Matrix4x4.CreateRotationY((float)(steering.SteeringAngle * dir));
            
        constraint.Axis = Vector3.Transform(constraint.Axis, rotationMatrix);
        constraint.AxisB = Vector3.Transform(constraint.AxisB, rotationMatrix);
    }

    private void SpeedupWheel(EntityUid wheelUid, EntityUid carUid, float dir)
    {
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
