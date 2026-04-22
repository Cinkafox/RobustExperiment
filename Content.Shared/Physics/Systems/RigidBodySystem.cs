using Content.Shared.Physics.Components;
using Robust.Shared.Prototypes;
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
    }

    public void ApplyForce(Entity<RigidBodyComponent> ent, Vector3 force)
    {
        if(ent.Comp.PhysType == PhysType.Static) return;
        ent.Comp.LinearVelocity += force / ent.Comp.Mass;
    }

    public void ApplyAngularForce(Entity<RigidBodyComponent> ent, Vector3 force)
    {
        if(ent.Comp.PhysType == PhysType.Static) return;
        ent.Comp.AngularVelocity += force / ent.Comp.Mass;
    }

    public override void FrameUpdate(float frameTime)
    {
        SimulateStep(frameTime);
    }
}