using Robust.Client.Graphics;

namespace Content.Client.StyleSheet.StyleBox;

[Serializable, DataDefinition]
public sealed partial class StyleBoxFlatData : StyleBoxData
{
    [DataField] public Color BackgroundColor;
    [DataField] public Color BorderColor;

    /// <summary>
    /// Thickness of the border, in virtual pixels.
    /// </summary>
    [DataField] public Thickness BorderThickness;

    public static implicit operator StyleBoxFlat(StyleBoxFlatData data)
    {
        var styleBox = new StyleBoxFlat();
        data.SetBaseParam(ref styleBox);
        styleBox.BackgroundColor = data.BackgroundColor;
        styleBox.BorderColor = data.BorderColor;
        styleBox.BorderThickness = data.BorderThickness;
        return styleBox;
    }
}