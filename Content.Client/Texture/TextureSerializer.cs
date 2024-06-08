using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Client.Texture;

[TypeSerializer]
public sealed class TextureSerializer : ITypeSerializer<Robust.Client.Graphics.Texture, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public Robust.Client.Graphics.Texture Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Robust.Client.Graphics.Texture>? instanceProvider = null)
    {
        var path = serializationManager.Read<ResPath>(node);
        var tr = new TextureResource();
        tr.Load(dependencies,path);
        return tr.Texture;
    }

    public DataNode Write(ISerializationManager serializationManager, Robust.Client.Graphics.Texture value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}