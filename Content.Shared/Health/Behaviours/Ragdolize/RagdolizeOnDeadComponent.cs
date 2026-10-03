using Content.Shared.Bone;

namespace Content.Shared.Health.Behaviours.Ragdolize;

[RegisterComponent]
public sealed partial class RagdolizeOnDeadComponent : Component
{
    [DataField] public Dictionary<string, BonePhysicsProperty> BonePhysics = [];
}