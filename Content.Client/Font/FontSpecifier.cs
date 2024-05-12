using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
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
public partial class FontSpecifier : Robust.Client.Graphics.Font
{
    [DataField] public int Size;
    [DataField("font")] public ResPath Path;

    public FontSpecifier()
    {
    }

    public FontSpecifier(ResPath resPath, int size)
    {
        Path = resPath;
        Size = size;
    }
    
    protected Robust.Client.Graphics.Font? _font;
    protected FontResource? _fontResource;

    public FontResource FontResource
    {
        get
        {
            if(_fontResource == null) 
                _fontResource = IoCManager.Resolve<IResourceCache>().GetResource<FontResource>(Path);
            return _fontResource;
        }
    }

    public virtual Robust.Client.Graphics.Font GetFont()
    {
        if (_font != null) return _font;
            
        _font = new VectorFont(FontResource, Size);

        return _font;
    }
    
    public static implicit operator FontResource(FontSpecifier fontSpecifier) => fontSpecifier.FontResource;
    public override int GetAscent(float scale)
    {
        return GetFont().GetAscent(scale);
    }

    public override int GetHeight(float scale)
    {
        return GetFont().GetHeight(scale);
    }

    public override int GetDescent(float scale)
    {
        return GetFont().GetDescent(scale);
    }

    public override int GetLineHeight(float scale)
    {
        return GetFont().GetLineHeight(scale);
    }

    public override float DrawChar(DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback = true)
    {
        return GetFont().DrawChar(handle, rune, baseline, scale, color, fallback);
    }

    public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback = true)
    {
        return GetFont().GetCharMetrics(rune, scale, fallback);
    }
}

public sealed class StackedFontSpecifier : FontSpecifier
{
    [DataField] public List<FontSpecifier> Specifiers = new();

    public override Robust.Client.Graphics.Font GetFont()
    {
        if (_font != null) return _font;
            
        _font = new StackedFont(Specifiers.Select(s => s.GetFont()).ToArray());
        return _font;
    }
}

[TypeSerializer]
public sealed class FontSerializer : ITypeSerializer<Robust.Client.Graphics.Font, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public Robust.Client.Graphics.Font Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Robust.Client.Graphics.Font>? instanceProvider = null)
    {
        if (!node.TryGet("font", out var pathNode) || !node.TryGet("size", out var sizeNode))
            throw new Exception();
        var path = serializationManager.Read<ResPath>(pathNode);
        var size = serializationManager.Read<int>(sizeNode);

        return new FontSpecifier(path, size);
    }

    public DataNode Write(ISerializationManager serializationManager, Robust.Client.Graphics.Font value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}