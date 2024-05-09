using Robust.Client.Graphics;

namespace Content.Client.StyleSheet.StyleBox;

[Serializable, DataDefinition]
public sealed partial class StyleBoxEmptyData : StyleBoxData
{
    public static implicit operator StyleBoxEmpty(StyleBoxEmptyData data)
    {
        var box = new StyleBoxEmpty();
        data.SetBaseParam(ref box);
        return box;
    }
}