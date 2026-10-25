using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraComponent : Component
{
    [DataField, AutoNetworkedField] public float FoV = 3;
    [DataField, AutoNetworkedField] public Vector3 Shift = Vector3.Zero;
    [DataField, AutoNetworkedField] public EulerAngles Rotation = EulerAngles.Zero;
}