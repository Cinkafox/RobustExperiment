using System.IO;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Client.Font;


[Serializable, DataDefinition, Virtual]
public partial class FontSpecifier
{
    [DataField] public int Size;
    [DataField] public ResPath Path;
    
    protected Robust.Client.Graphics.Font? _font;
    public new Robust.Client.Graphics.Font Font
    {
        get
        {
            if (_font != null) return _font;
            var fontResource = IoCManager.Resolve<IResourceCache>().GetResource<FontResource>(Path);
            _font = new VectorFont(fontResource, Size);

            return _font;
        }
    }
    public static implicit operator Robust.Client.Graphics.Font(FontSpecifier fontSpecifier) => fontSpecifier.Font;
}

public sealed class StackedFontSpecifier : FontSpecifier
{
    [DataField] public List<FontSpecifier> Specifiers = new();
    
    public new Robust.Client.Graphics.Font Font
    {
        get
        {
            if (_font != null) return _font;
            
            _font = new StackedFont(Specifiers.Select(s => s.Font).ToArray());
            return _font;
        }
    }
}