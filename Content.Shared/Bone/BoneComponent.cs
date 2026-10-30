using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Bone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BoneComponent : Component
{
    [DataField, AutoNetworkedField] public HashSet<BoneVertexData> BoneVertexDatum = new();
    [DataField, AutoNetworkedField] public HashSet<EntityUid> Children = new();

    [DataField, AutoNetworkedField] public Vector4 OriginalPosition;
    [DataField, AutoNetworkedField] public Quaternion OriginalRotation;
    [ViewVariables, AutoNetworkedField] public EntityUid Skeleton;
}