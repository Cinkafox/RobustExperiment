using System.IO;
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

public sealed class FontSpecifier
{
    public Robust.Client.Graphics.Font Font;

    public FontSpecifier(Robust.Client.Graphics.Font font)
    {
        Font = font;
    }

    public static implicit operator Robust.Client.Graphics.Font(FontSpecifier fontSpecifier) => fontSpecifier.Font;
}

[TypeSerializer]
public sealed class FontSpecifierSerializer : ITypeSerializer<FontSpecifier, MappingDataNode>,
    ITypeSerializer<FontSpecifier, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var path = serializationManager.Read<ResPath>(node["font"]);
        var size = serializationManager.Read<int>(node["size"]);

        if (!dependencies.Resolve<IResourceCache>().TryGetResource<FontResource>(path, out _))
            return new ErrorNode(node["font"], "Font not found!");
        if (size <= 0) return new ErrorNode(node["size"], "Size must bigger 0");
        return new ValidatedMappingNode(new()
        {
            {new ValidatedValueNode(new ValueDataNode("font")), new ValidatedValueNode(node["font"])},
            {new ValidatedValueNode(new ValueDataNode("size")), new ValidatedValueNode(node["size"])}
        });
    }

    public FontSpecifier Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<FontSpecifier>? instanceProvider = null)
    {
        var path = serializationManager.Read<ResPath>(node["font"]);
        var size = serializationManager.Read<int>(node["size"]);
        if (!dependencies.Resolve<IResourceCache>().TryGetResource<FontResource>(path, out var fontResource))
            throw new FileNotFoundException($"Font {path} not exist");

        return new FontSpecifier(new VectorFont(fontResource, size));
    }

    public DataNode Write(ISerializationManager serializationManager, FontSpecifier value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();
        foreach (var dataNode in node)
        {
            if (dataNode is not MappingDataNode mappingDataNode)
                return new ErrorNode(dataNode, "Node is not mappingNode");
            list.Add(Validate(serializationManager, mappingDataNode, dependencies, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public FontSpecifier Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<FontSpecifier>? instanceProvider = null)
    {
        var fontList = new List<Robust.Client.Graphics.Font>();
        foreach (var dataNode in node)
        {
            var specifier = serializationManager.Read<FontSpecifier?>(dataNode);
            if (specifier is null) throw new NullNotAllowedException();
            fontList.Add(specifier.Font);
        }

        return new FontSpecifier(new StackedFont(fontList.ToArray()));
    }
}