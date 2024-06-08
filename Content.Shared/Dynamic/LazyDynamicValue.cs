using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.Dynamic;

public class LazyDynamicValue
{
    private readonly Type _type;
    private readonly ISerializationManager _serializationManager;
    private readonly DataNode _node;
    private readonly ISerializationContext? _context;

    private object? _object;
    public object Object
    {
        get
        {
            if (_object is null)
            {
                _object = _serializationManager.Read(_type, _node)!;
            }

            return _object;
        }
    }

    public LazyDynamicValue(Type type,ISerializationManager serializationManager ,DataNode node, ISerializationContext? context)
    {
        _type = type;
        _serializationManager = serializationManager;
        _node = node;
        _context = context;
    }
    
}

public sealed class LazyDynamicValue<T> : LazyDynamicValue
{
    public LazyDynamicValue(DataNode node,ISerializationManager serializationManager, ISerializationContext? context) : base(typeof(T),serializationManager, node, context)
    {
    }
}

