using Content.Shared.Utils;

namespace Content.Client.Bone;

[RegisterComponent]
public sealed partial class SkeletonComponent : Component
{
    [DataField] public BoneCompound? Compound;
    
    [DataField] public EntityUid Root;
}

[DataDefinition]
public sealed partial class BoneCompound
{
    [DataField(required:true)] public Vector3 Position;
    [DataField(required:true)] public EulerAngles Rotation;
    [DataField] public HashSet<BoneVertexData>? Data;
    [DataField] public HashSet<BoneCompound>? Child;
}