using Content.Shared.Utils;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class BoneComponent : Component
{
    [DataField] public HashSet<BoneVertexData> BoneVertexDatum = new();
    [DataField] public HashSet<EntityUid> Childs = new();

    [DataField] public Vector4 OriginalPosition;
    [DataField] public EulerAngles OriginalRotation;
}