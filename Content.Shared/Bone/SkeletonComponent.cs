using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Bone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SkeletonComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid Root;
    [ViewVariables, AutoNetworkedField] public Matrix4x4 OriginalWorldMatrix = Matrix4x4.Identity;
    [ViewVariables, AutoNetworkedField] public readonly Dictionary<string, EntityUid> BonesDictionary = [];
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BoneCompoundComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid? CompoundEnt;
    [DataField] public EntProtoId<BoneCompoundComponent>? CompoundEntId;
    [DataField] public BoneCompound? Compound;
    [DataField] public Vector3 Offset = Vector3.Zero;
}