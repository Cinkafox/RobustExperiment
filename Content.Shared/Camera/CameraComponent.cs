using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraComponent : Component
{
    [DataField] public float FoV = 3;
    
    [ViewVariables] public EulerAngles AngleAcceleration = EulerAngles.Zero;
    [ViewVariables] public Vector3 CameraAcceleration = Vector3.Zero;
}