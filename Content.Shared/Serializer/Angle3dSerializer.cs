using System.Globalization;
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
public sealed class Angle3dSerializer : ITypeSerializer<Angle3d, ValueDataNode> , ITypeCopyCreator<Angle3d>
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
            : new ErrorNode(node, "Failed parsing values for Angle3d.");
    }

    public Angle3d Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Angle3d>? instanceProvider = null)
    {
        if (!VectorSerializerUtility.TryParseArgs(node.Value, 3, out var args))
        {
            throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{node.Value}'");
        }

        var pitch = serializationManager.Read<Angle>(new ValueDataNode(args[0]));
        var yaw = serializationManager.Read<Angle>(new ValueDataNode(args[1]));
        var roll = serializationManager.Read<Angle>(new ValueDataNode(args[2]));

        return new Angle3d(pitch, yaw, roll);
    }

    public DataNode Write(ISerializationManager serializationManager, Angle3d value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode($"{serializationManager.WriteValue<Angle>(value.Pitch)}," +
                                 $"{serializationManager.WriteValue<Angle>(value.Yaw)}," +
                                 $"{serializationManager.WriteValue<Angle>(value.Roll)}");
    }

    public Angle3d CreateCopy(ISerializationManager serializationManager, Angle3d source, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new Angle3d(source.Pitch, source.Yaw, source.Roll);
    }
}