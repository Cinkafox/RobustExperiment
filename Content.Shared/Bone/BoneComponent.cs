using Content.Shared.Utils;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class BoneComponent : Component
{
    [DataField] public HashSet<BoneVertexData> BoneVertexDatum = new();
    [DataField] public HashSet<EntityUid> Childs = new();

    [DataField] public Vector3 OriginalPosition;
    [DataField] public EulerAngles OriginalRotation;
}

[DataDefinition]
public sealed partial class BoneVertexData
{
    [DataField(required: true)] public int BoneIndices;
    [DataField(required:true)] public float BoneWeights;
}
