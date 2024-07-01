using Robust.Shared.Utility;

namespace Content.Shared.StackSpriting;

[RegisterComponent]
public sealed partial class StackSpriteComponent : Component
{
    [DataField] public ResPath Path;
    [DataField] public Vector2i Size;
}