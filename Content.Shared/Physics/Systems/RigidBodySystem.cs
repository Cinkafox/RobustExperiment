using Content.Shared.Debug;
using Content.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem : EntitySystem
{
    [Dependency] private readonly DebugSystem _debugSystem = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
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

    public override void Update(float frameTime)
    {
        SimulateStep(frameTime);
    }

    public FrictionData GetCombinedFriction(ProtoId<SurfacePrototype> surfaceA, ProtoId<SurfacePrototype> surfaceB)
    {
        if (!_prototypeManager.TryIndex(surfaceA, out var prototypeA) ||
            !_prototypeManager.TryIndex(surfaceB, out var prototypeB))
            return default;
        
        var aFriction = prototypeA.DefaultFriction;
        var bFriction = prototypeB.DefaultFriction;
        
        return new FrictionData(
            MathF.Sqrt(aFriction.StaticFriction * bFriction.StaticFriction),
            MathF.Sqrt(aFriction.DynamicFriction * bFriction.DynamicFriction)
        );
    }
}