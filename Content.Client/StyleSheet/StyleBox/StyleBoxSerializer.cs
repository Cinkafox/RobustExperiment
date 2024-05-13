using Robust.Client.Graphics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.StyleSheet.StyleBox;

[TypeSerializer]
public sealed class StyleBoxSerializer : ITypeSerializer<StyleBoxFlat, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<StyleBoxFlat>(node);
    }

    public StyleBoxFlat Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<StyleBoxFlat>? instanceProvider = null)
    {
        return serializationManager.Read<StyleBoxFlatData?>(node)!;
    }

    public DataNode Write(ISerializationManager serializationManager, StyleBoxFlat value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}