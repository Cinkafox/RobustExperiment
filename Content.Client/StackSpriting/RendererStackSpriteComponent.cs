namespace Content.Client.StackSpriting;

[RegisterComponent]
public sealed partial class RendererStackSpriteComponent : Component
{
    [ViewVariables] public Vector2i Size;
    [ViewVariables] public int Height;
    public Robust.Client.Graphics.Texture Texture;
}