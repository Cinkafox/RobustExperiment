using Vector3 = System.Numerics.Vector3;

namespace Content.Client.StackSpriting;

[RegisterComponent]
public sealed partial class RendererStackSpriteComponent : Component
{
    [ViewVariables] public Vector2i Size;
    [ViewVariables] public int Height;
    [ViewVariables] public Vector3? Center;
    public Robust.Client.Graphics.Texture Texture;
}