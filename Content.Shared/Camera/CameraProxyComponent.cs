namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraProxyComponent: Component
{
    [ViewVariables] public EntityUid CameraUid;
}