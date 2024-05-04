using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.SkyBoxes;

[RegisterComponent]
public sealed partial class SkyBoxComponent: Component
{
    [DataField(required:true)] public SpriteSpecifier SkyboxPath;
    public Texture Texture;
}