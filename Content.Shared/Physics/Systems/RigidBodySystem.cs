using Content.Shared.Debug;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem : EntitySystem
{
    [Dependency] private readonly DebugSystem _debugSystem = default!;
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

    public void Solve(Entity<RigidBodyComponent> ent)
    {
        var query = EntityQueryEnumerator<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var rigidBodyComponent))
        {
            ApplyForce(new Entity<RigidBodyComponent>(uid, rigidBodyComponent), new Vector3(0,-0.0025f,0) * rigidBodyComponent.Mass);
            
            if(ent.Owner == uid) 
                continue;
            
            SolveCollide(ent, new Entity<RigidBodyComponent>(uid, rigidBodyComponent));
        }
    }

    public void Simulate()
    {
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var rigidBodyComponent, out var transform3dComponent))
        {
            Solve(new Entity<RigidBodyComponent>(uid, rigidBodyComponent));
        }
        
        query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        
        while (query.MoveNext(out var uid, out var rigidBodyComponent, out var transform3dComponent))
        {
            if(rigidBodyComponent.PhysType == PhysType.Static) continue;
            
            transform3dComponent.LocalPosition += rigidBodyComponent.LinearVelocity;
            //transform3dComponent.LocalRotation += new EulerAngles(rigidBodyComponent.LinearVelocity.X, rigidBodyComponent.LinearVelocity.Y, rigidBodyComponent.LinearVelocity.Z).ToQuaternion();
        }
    }

    private int SkipDuration = 10;

    public override void Update(float frameTime)
    {
        if(SkipDuration == 0 || true)
        {
            Simulate();
            SkipDuration = 10;
        }
        
        SkipDuration--;
    }
}