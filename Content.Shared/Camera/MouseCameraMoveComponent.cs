using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class MouseCameraMoveComponent: Component
{
    [ViewVariables] public EulerAngles CameraRotation = EulerAngles.Zero;
    [DataField] public Angle AutoTurnThreshold = Angle.FromDegrees(55);
}