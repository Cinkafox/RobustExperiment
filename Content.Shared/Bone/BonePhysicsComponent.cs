using Content.Shared.Physics.Components;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class BonePhysicsComponent : Component
{
    [DataField] public Dictionary<string, PhysicsProperty> BonePhysics = [];
    [DataField] public Dictionary<EntityUid, EntityUid> Proxies = [];
}

[RegisterComponent]
public sealed partial class BoneProxyComponent : Component
{
    [ViewVariables] public EntityUid ProxyUid;
    [ViewVariables] public EntityUid OwnerUid;
}