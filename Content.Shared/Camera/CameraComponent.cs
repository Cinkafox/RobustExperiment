using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraComponent : Component
{
    [DataField] public float FoV = 3;
    [DataField] public Vector3 Shift = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
}