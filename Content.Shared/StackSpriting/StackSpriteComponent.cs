using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared.StackSpriting;

[RegisterComponent]
public sealed partial class StackSpriteComponent : Component
{
    [DataField(required:true)] public SpriteSpecifier Sprite;
}