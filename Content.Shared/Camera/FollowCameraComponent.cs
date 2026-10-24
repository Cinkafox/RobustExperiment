using Content.Shared.Utils;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FollowCameraComponent: Component
{
    [ViewVariables, AutoNetworkedField] public Dictionary<EntityUid, FollowCameraEntry> CameraQueue = [];
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class FollowCameraEntry
{
    [DataField] public Vector3 Offset = new Vector3(0,3,0);
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public bool RotateByEntity;
}