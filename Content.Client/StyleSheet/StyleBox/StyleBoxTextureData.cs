using System.Numerics;
using Robust.Client.Utility;
using Robust.Shared.Utility;
using StyleBoxTexture = Robust.Client.Graphics.StyleBoxTexture;

namespace Content.Client.StyleSheet.StyleBox;

[Serializable, DataDefinition]
public sealed partial class StyleBoxTextureData : StyleBoxData
{
    [DataField] public SpriteSpecifier Texture;
    
    /// <summary>
    /// Left expansion size, in virtual pixels.
    /// </summary>
    /// <remarks>
    /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
    /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
    /// <see cref="TextureScale"/>.
    /// </remarks>
    [DataField] public float ExpandMarginLeft;
    /// <summary>
    /// Top expansion size, in virtual pixels.
    /// </summary>
    /// <remarks>
    /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
    /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
    /// <see cref="TextureScale"/>.
    /// </remarks>
    [DataField] public float ExpandMarginTop;

    /// <summary>
    /// Bottom expansion size, in virtual pixels.
    /// </summary>
    /// <remarks>
    /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
    /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
    /// <see cref="TextureScale"/>.
    /// </remarks>
    [DataField] public float ExpandMarginBottom ;

    /// <summary>
    /// Right expansion size, in virtual pixels.
    /// </summary>
    /// <remarks>
    /// This expands the size of the area where the patches get drawn. This will cause the drawn texture to
    /// extend beyond the box passed to the <see cref="StyleBox.Draw"/> function. This is not affected by
    /// <see cref="TextureScale"/>.
    /// </remarks>
    [DataField] public float ExpandMarginRight;

    [DataField] public StyleBoxTexture.StretchMode Mode = StyleBoxTexture.StretchMode.Stretch;

    /// <summary>
    /// Distance of the left patch margin from the image. In texture space.
    /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
    /// </summary>
    [DataField] public float PatchMarginLeft;
    /// <summary>
    /// Distance of the right patch margin from the image. In texture space.
    /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
    /// </summary>
    [DataField] public float PatchMarginRight;

    /// <summary>
    /// Distance of the top patch margin from the image. In texture space.
    /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
    /// </summary>
    [DataField] public float PatchMarginTop;

    /// <summary>
    /// Distance of the bottom patch margin from the image. In texture space.
    /// The size of this patch in virtual pixels can be obtained by scaling this with <see cref="TextureScale"/>.
    /// </summary>
    [DataField] public float PatchMarginBottom;

    [DataField] public Thickness? PatchMargin;
    [DataField] public Thickness? ExpandMargin;

    [DataField] public Color Modulate = Color.White;
    
    /// <summary>
    /// Additional scaling to use when drawing the texture.
    /// </summary>
    [DataField] public Vector2 TextureScale  = Vector2.One;
    
    public static implicit operator StyleBoxTexture(StyleBoxTextureData styleBoxData)
    {
        var styleBox = new StyleBoxTexture();
        styleBoxData.SetBaseParam(ref styleBox);
        styleBox.Texture = styleBoxData.Texture.Frame0();
        styleBox.Mode = styleBoxData.Mode;
        styleBoxData.Modulate = styleBoxData.Modulate;
        styleBoxData.TextureScale = styleBoxData.TextureScale;

        if (styleBoxData.ExpandMargin is null)
        {
            styleBox.ExpandMarginBottom = styleBoxData.ExpandMarginBottom;
            styleBox.ExpandMarginTop = styleBoxData.ExpandMarginTop;
            styleBox.ExpandMarginRight = styleBoxData.ExpandMarginRight;
            styleBox.ExpandMarginLeft = styleBoxData.ExpandMarginLeft;
        }
        else
        {
            styleBox.ExpandMarginBottom = styleBoxData.ExpandMargin.Value.Bottom;
            styleBox.ExpandMarginTop = styleBoxData.ExpandMargin.Value.Top;
            styleBox.ExpandMarginRight = styleBoxData.ExpandMargin.Value.Right;
            styleBox.ExpandMarginLeft = styleBoxData.ExpandMargin.Value.Left;
        }

        if (styleBoxData.PatchMargin is null)
        {
            styleBoxData.PatchMarginBottom = styleBox.PatchMarginBottom;
            styleBoxData.PatchMarginTop = styleBox.PatchMarginTop;
            styleBoxData.PatchMarginRight = styleBox.PatchMarginRight;
            styleBoxData.PatchMarginLeft = styleBox.PatchMarginLeft;
        }
        else
        {
            styleBoxData.PatchMarginBottom = styleBoxData.PatchMargin.Value.Bottom;
            styleBoxData.PatchMarginTop = styleBoxData.PatchMargin.Value.Top;
            styleBoxData.PatchMarginRight = styleBoxData.PatchMargin.Value.Right;
            styleBoxData.PatchMarginLeft = styleBoxData.PatchMargin.Value.Left;
        }

        return styleBox;
    }
}