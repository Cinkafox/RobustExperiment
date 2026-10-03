using Content.Shared.Bone;

namespace Content.Shared.Health.Behaviours.Ragdolize;

public sealed class RagdolizeOnDeadSystem : EntitySystem
{
    [Dependency] private readonly BonePhysicsSystem _bonePhysicsSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RagdolizeOnDeadComponent, OnEntityHealthStatusEvent>(OnHealthStatusChange);
    }

    private void OnHealthStatusChange(Entity<RagdolizeOnDeadComponent> ent, ref OnEntityHealthStatusEvent args)
    {
        _bonePhysicsSystem.AddRagdoll(ent.Owner, ent.Comp.BonePhysics);
    }
}