using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Client.StyleSheet.Dynamic;

[TypeSerializer]
public sealed class DynamicValueSerializer : ITypeSerializer<DynamicValue, MappingDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedMappingNode([]);
    }

    public DynamicValue Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DynamicValue>? instanceProvider = null)
    {
        if (!node.TryGet("valueType", out ValueDataNode? valueType) || !node.TryGet("value", out var value))
            throw new InvalidMappingException("IDI NAHYI");
        
        var type = dependencies.Resolve<IReflectionManager>().GetType(valueType.Value);
        if (type is null)
        {
            throw new InvalidMappingException("NO TYPE " + valueType.Value);
        }
        
        return new DynamicValue(valueType.Value, serializationManager.Read(type, value, context)!);
    }

    public DataNode Write(ISerializationManager serializationManager, DynamicValue value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }
}