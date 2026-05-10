using Content.Shared.Physics.Components;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem : EntitySystem
{
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    
    public override void Initialize()
    {
        InitializeColliders();
        SimulateStep(10f, 4);
    }

    public void ApplyForce(Entity<RigidBodyComponent> ent, Vector3 force)
    {
        if(ent.Comp.PhysType == PhysType.Static) return;
        ent.Comp.LinearVelocity += force / ent.Comp.Mass;
    }

    public void ApplyTorque(Entity<RigidBodyComponent> ent, Vector3 torque)
    {
        if (ent.Comp.PhysType == PhysType.Static)
            return;

        if (!ent.Comp.EnableAngularVelocity)
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

    public override void FrameUpdate(float frameTime)
    {
        SimulateStep(frameTime, 4);
    }
}