using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class MouseCameraMoveComponent: Component
{
    [ViewVariables] public EulerAngles CameraRotation = EulerAngles.Zero;

    [DataField] public bool EnableBodyAutoTurn = true;
    [DataField] public Angle AutoTurnThreshold = Angle.FromDegrees(55);
    [DataField] public Angle AutoTurnSpeed = Angle.FromDegrees(200);
    [DataField] public bool AutoTurnOnlyWhenMoving = true;
}