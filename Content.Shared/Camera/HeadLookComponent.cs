using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeadLookComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EulerAngles Rotation = EulerAngles.Zero;
}
