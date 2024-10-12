using Content.Shared.Utils;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.Serializer;

[TypeSerializer]
public sealed class EulerAnglesSerializer : ITypeSerializer<EulerAngles, ValueDataNode> , ITypeCopyCreator<EulerAngles>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (!VectorSerializerUtility.TryParseArgs(node.Value, 3, out var args))
        {
            throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{node.Value}'");
        }

        return serializationManager.ValidateNode<Angle>(new ValueDataNode(args[0])).Valid && 
               serializationManager.ValidateNode<Angle>(new ValueDataNode(args[1])).Valid && 
               serializationManager.ValidateNode<Angle>(new ValueDataNode(args[2])).Valid
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing values for EulerAngles.");
    }

    public EulerAngles Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<EulerAngles>? instanceProvider = null)
    {
        if (!VectorSerializerUtility.TryParseArgs(node.Value, 3, out var args))
        {
            throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{node.Value}'");
        }

        var pitch = serializationManager.Read<Angle>(new ValueDataNode(args[0]));
        var yaw = serializationManager.Read<Angle>(new ValueDataNode(args[1]));
        var roll = serializationManager.Read<Angle>(new ValueDataNode(args[2]));

        return new EulerAngles(pitch, yaw, roll);
    }

    public DataNode Write(ISerializationManager serializationManager, EulerAngles value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode($"{serializationManager.WriteValue<Angle>(value.Pitch)}," +
                                 $"{serializationManager.WriteValue<Angle>(value.Yaw)}," +
                                 $"{serializationManager.WriteValue<Angle>(value.Roll)}");
    }

    public EulerAngles CreateCopy(ISerializationManager serializationManager, EulerAngles source, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new EulerAngles(source.Pitch, source.Yaw, source.Roll);
    }
}