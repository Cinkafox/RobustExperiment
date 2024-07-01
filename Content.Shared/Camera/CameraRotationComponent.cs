using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[RegisterComponent]
public sealed partial class CameraRotationComponent : Component
{
    public CameraRotation CurrentRotation;
}

[Flags]
[Serializable, NetSerializable]
public enum CameraRotation : byte
{
    None = 0,
    Left = 1,
    Right = 2,
}