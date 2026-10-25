using Content.Shared.Physics.Components;
using Content.Shared.Transform;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem : EntitySystem
{
    [Dependency] private ISandboxHelper _sandboxHelper = default!;
    [Dependency] private IReflectionManager _reflectionManager = default!;
    
    public override void Initialize()
    {
        EntityManager.ComponentFactory.RegisterNetworkedFields<RigidBodyComponent>(
            nameof(RigidBodyComponent.LinearVelocity),
            nameof(RigidBodyComponent.PhysType),
            nameof(RigidBodyComponent.Density),
            nameof(RigidBodyComponent.AngularVelocity),
            nameof(RigidBodyComponent.EnableAngularVelocity),
            nameof(RigidBodyComponent.Friction),
            nameof(RigidBodyComponent.Restitution),
            nameof(RigidBodyComponent.RollingResistance));
        
        InitializeColliders();
        //SimulateStep(5000f, 100);
    }

    public void ApplyForce(Entity<RigidBodyComponent> ent, Vector3 force)
    {
        if(ent.Comp.PhysType == PhysType.Static) return;
        ent.Comp.LinearVelocity += force / ent.Comp.Mass;
    }
    
    public void ApplyRelativeTorque(Entity<RigidBodyComponent> ent, Vector3 localTorque)
    {
        var rotation = Comp<Transform3dComponent>(ent).LocalRotation;
        var worldTorque = Vector3.Transform(localTorque, rotation);
        
        ApplyTorque(ent, worldTorque);
    }


    public void ApplyTorque(Entity<RigidBodyComponent> ent, Vector3 torque)
    {
        if (ent.Comp.PhysType == PhysType.Static)
            return;

        if (!ent.Comp.EnableAngularVelocity)
            return;
        
        if(torque.LengthSquared() <= 0.01f) 
            return;

        ent.Comp.AngularVelocity += torque * ent.Comp.InvInertia;
    }
    
    public void ApplyForceAtPoint(
        Entity<RigidBodyComponent> ent,
        Vector3 force,
        Vector3 worldPoint,
        Vector3 centerOfMass)
    {
        if (ent.Comp.PhysType == PhysType.Static)
            return;

        ApplyForce(ent, force);

        var r = worldPoint - centerOfMass;
        var torque = Vector3.Cross(r, force);

        ApplyTorque(ent, torque);
    }

    public override void Update(float frameTime)
    {
        SimulateStep(frameTime, 8);
    }
}