using Robust.Shared.Utility;
using Vector3 = System.Numerics.Vector3;

namespace Content.Shared.StackSpriting;

[RegisterComponent]
public sealed partial class StackSpriteComponent : Component
{
    [DataField] public ResPath Path;
    [DataField] public Vector2i Size;
    [DataField] public Vector3? Center;
}