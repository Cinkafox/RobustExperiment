using Robust.Shared.Prototypes;
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
public sealed class DynamicValueSerializer : ITypeSerializer<DynamicValue, MappingDataNode>, ITypeSerializer<DynamicValue, ValueDataNode>
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
            throw new InvalidMappingException("Mappings not found");

        if (valueType.Value == DynamicValue.ReadByPrototypeCommand)
            return serializationManager.Read<DynamicValue?>(value) ?? throw new InvalidOperationException();
        
        dependencies.Resolve<ILogManager>().GetSawmill("DynShit").Debug($"FFUCKING SHIT MEOWS ON ME {valueType.Value} {typeof(Color).FullName}");
        
        var type = serializationManager.Read<Type?>(valueType);
        if (type is null)
            throw new InvalidMappingException("NO TYPE " + valueType.Value);
        
        return new DynamicValue(valueType.Value, serializationManager.Read(type, value, context)!);
    }

    public DataNode Write(ISerializationManager serializationManager, DynamicValue value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }

    public DynamicValue Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<DynamicValue>? instanceProvider = null)
    {
        var value = serializationManager.Read<string?>(node);
        if (value is null) throw new Exception("FUCK!");

        if (value[0] == '#')
        {
            var color = serializationManager.Read<Color>(node);
            return new DynamicValue("Color", color);
        }

        return new LazyDynamicValue(serializationManager.Read<ProtoId<DynamicValuePrototype>>(node));
    }
}