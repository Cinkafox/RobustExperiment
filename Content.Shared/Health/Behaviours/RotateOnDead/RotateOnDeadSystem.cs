using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Shared.Health.Behaviours.RotateOnDead;

public sealed class RotateOnDeadSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<RotateOnDeadComponent, OnEntityHealthStatusEvent>(OnHealthStatusChange);
    }

    private void OnHealthStatusChange(Entity<RotateOnDeadComponent> ent, ref OnEntityHealthStatusEvent args)
    {
        if(!TryComp<Transform3dComponent>(ent, out var transform)) 
            return;
        
        transform.LocalRotation *= (new EulerAngles(-Angle.FromDegrees(90), 0, 0)).ToQuaternion();
    }
}