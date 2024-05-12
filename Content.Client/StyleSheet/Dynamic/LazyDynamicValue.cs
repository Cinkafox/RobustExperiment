using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Client.StyleSheet.Dynamic;

[DataDefinition, Serializable]
public sealed partial class LazyDynamicValue : DynamicValue
{
    private DynamicValue? _other;
    public ProtoId<DynamicValuePrototype> ProtoId;
    public DynamicValue Other
    {
        get
        {
            if (_other == null)
                _other = IoCManager.Resolve<IPrototypeManager>().Index(ProtoId).Value;
            
            return _other;
        }
    }

    public LazyDynamicValue(ProtoId<DynamicValuePrototype> protoId)
    {
        ProtoId = protoId;
    }

    public override object GetValueObject()
    {
        return Other.GetValueObject();
    }

    public override string GetValueType()
    {
        return Other.GetValueType();
    }
}