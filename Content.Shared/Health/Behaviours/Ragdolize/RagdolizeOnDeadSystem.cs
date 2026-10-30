using Content.Shared.Bone;
using Robust.Shared.Timing;

namespace Content.Shared.Health.Behaviours.Ragdolize;

public sealed partial class RagdolizeOnDeadSystem : EntitySystem
{
    [Dependency] private BonePhysicsSystem _bonePhysicsSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<RagdolizeOnDeadComponent, OnEntityHealthStatusEvent>(OnHealthStatusChange);
    }

    private void OnHealthStatusChange(Entity<RagdolizeOnDeadComponent> ent, ref OnEntityHealthStatusEvent args)
    {
        if(_gameTiming.IsFirstTimePredicted)
            _bonePhysicsSystem.AddRagdoll(ent.Owner, ent.Comp.BonePhysics);
    }
}