using Content.Shared.Utils;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraProxyComponent: Component
{
    [ViewVariables] public EntityUid CameraUid;
}

[RegisterComponent]
public sealed partial class FollowCameraComponent: Component
{
    [ViewVariables] public EntityUid EntityUid;
    [DataField] public Vector3 Offset = new Vector3(0,3,0);
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
}

[RegisterComponent]
public sealed partial class FollowCameraCreateComponent: Component
{
    [DataField] public Vector3 Offset = new Vector3(0,3,0);
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
}