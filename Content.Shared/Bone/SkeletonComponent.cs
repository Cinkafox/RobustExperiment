using System.Numerics;
using Content.Shared.Utils;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class SkeletonComponent : Component
{
    [DataField] public BoneCompound? Compound;
    [DataField] public EntityUid Root;
    [DataField] public Vector3 Offset = Vector3.Zero;
}

[DataDefinition]
public sealed partial class BoneCompound
{
    [DataField(required:true)] public Vector3 Position;
    [DataField(required:true)] public EulerAngles Rotation;
    [DataField] public HashSet<BoneVertexData>? Data;
    [DataField] public HashSet<BoneCompound>? Child;
}