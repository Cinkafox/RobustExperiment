using System.Globalization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Shared.Serializer;

[TypeSerializer]
public sealed class ThicknessSerializer : ITypeSerializer<Thickness, ValueDataNode>, ITypeCopyCreator<Thickness>
{
    public Thickness Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<Thickness>? instanceProvider = null)
    {
        if (!VectorSerializerUtility.TryParseArgs(node.Value, 4, out var args))
        {
            throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{node.Value}'");
        }

        var x = float.Parse(args[0], CultureInfo.InvariantCulture);
        var y = float.Parse(args[1], CultureInfo.InvariantCulture);
        var z = float.Parse(args[2], CultureInfo.InvariantCulture);
        var w = float.Parse(args[3], CultureInfo.InvariantCulture);

        return new Thickness(x, y, z, w);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (!VectorSerializerUtility.TryParseArgs(node.Value, 4, out var args))
        {
            throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{node.Value}'");
        }

        return float.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
               float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
               float.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
               float.TryParse(args[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing values for Thickness.");
    }

    public DataNode Write(ISerializationManager serializationManager, Thickness value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode($"{value.Left.ToString(CultureInfo.InvariantCulture)}," +
                                 $"{value.Top.ToString(CultureInfo.InvariantCulture)}," +
                                 $"{value.Right.ToString(CultureInfo.InvariantCulture)}," +
                                 $"{value.Bottom.ToString(CultureInfo.InvariantCulture)}");
    }

    public Thickness CreateCopy(ISerializationManager serializationManager, Thickness source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new(source.Left,source.Top,source.Right,source.Bottom);
    }
}