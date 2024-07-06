using Content.Shared.Movement;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Vehicle;

public sealed partial class VehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    public override void Update(float frameTime)
    {
        UpdateControl(frameTime);

        var query = EntityQueryEnumerator<VehicleComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var vehicleComponent, out var transformComponent, out var physicsComponent))
        {
            var angle = - transformComponent.WorldRotation - Angle.FromDegrees(270);
            var velocity = angle.ToVec() * (float)(vehicleComponent.Power - vehicleComponent.Reverse);
            
            _physicsSystem.SetLinearVelocity(uid, velocity);
            _physicsSystem.SetAngularVelocity(uid, (float)vehicleComponent.AngularVelocity);
            _physicsSystem.SetAngularVelocity(uid, (float)(physicsComponent.AngularVelocity * vehicleComponent.Drag));

            vehicleComponent.AngularVelocity *= vehicleComponent.AngularDrag;
        }
    }
    
}