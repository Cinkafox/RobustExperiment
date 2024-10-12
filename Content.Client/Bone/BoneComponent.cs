using Content.Shared.Utils;

namespace Content.Client.Bone;

[RegisterComponent]
public sealed partial class BoneComponent : Component
{
    [DataField] public HashSet<BoneVertexData> BoneVertexDatum = new HashSet<BoneVertexData>();
    [DataField] public HashSet<EntityUid> Childs = new HashSet<EntityUid>();

    [DataField] public Vector3 OriginalPosition;
    [DataField] public EulerAngles OriginalRotation;
}

[DataDefinition]
public sealed partial class BoneVertexData
{
    [DataField(required: true)] public int BoneIndices;
    [DataField(required:true)] public float BoneWeights;
}
