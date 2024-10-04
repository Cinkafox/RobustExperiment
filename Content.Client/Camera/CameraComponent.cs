namespace Content.Client.Camera;

[RegisterComponent]
public sealed partial class CameraComponent : Component
{
    [DataField] public float FoV = 3;
}