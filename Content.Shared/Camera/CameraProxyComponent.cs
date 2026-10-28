using Robust.Shared.GameStates;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraProxyComponent: Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid CameraUid;
    [ViewVariables, AutoNetworkedField] public EntityUid FollowUid;
}