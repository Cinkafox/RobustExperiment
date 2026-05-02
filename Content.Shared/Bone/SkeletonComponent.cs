using Robust.Shared.Prototypes;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class SkeletonComponent : Component
{
    [ViewVariables] public EntityUid Root;
    [ViewVariables] public readonly Dictionary<string, EntityUid> BonesDictionary = [];
}

[RegisterComponent]
public sealed partial class BoneCompoundComponent : Component
{
    [ViewVariables] public EntityUid? CompoundEnt;
    [DataField] public EntProtoId<BoneCompoundComponent>? CompoundEntId;
    [DataField] public BoneCompound? Compound;
    [DataField] public Vector3 Offset = Vector3.Zero;
}