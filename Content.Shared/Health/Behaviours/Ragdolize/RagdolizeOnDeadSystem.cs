using Content.Shared.Bone;

namespace Content.Shared.Health.Behaviours.Ragdolize;

public sealed partial class RagdolizeOnDeadSystem : EntitySystem
{
    [Dependency] private BonePhysicsSystem _bonePhysicsSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RagdolizeOnDeadComponent, OnEntityHealthStatusEvent>(OnHealthStatusChange);
    }

    private void OnHealthStatusChange(Entity<RagdolizeOnDeadComponent> ent, ref OnEntityHealthStatusEvent args)
    {
        _bonePhysicsSystem.AddRagdoll(ent.Owner, ent.Comp.BonePhysics);
    }
}