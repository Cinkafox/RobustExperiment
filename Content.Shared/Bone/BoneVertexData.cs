using Robust.Shared.Serialization;

namespace Content.Shared.Bone;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BoneVertexData
{
    [DataField(required: true)] public int BoneIndices;
    [DataField(required: true)] public float BoneWeights;
}