using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class FollowCameraCreateComponent: Component
{
    [DataField] public Vector3 Offset = new Vector3(0,3,0);
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public bool RotateByEntity;
    [DataField] public string? Bone;
}