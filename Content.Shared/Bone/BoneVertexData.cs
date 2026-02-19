namespace Content.Shared.Bone;

[DataDefinition]
public sealed partial class BoneVertexData
{
    [DataField(required: true)] public int BoneIndices;
    [DataField(required: true)] public float BoneWeights;
}