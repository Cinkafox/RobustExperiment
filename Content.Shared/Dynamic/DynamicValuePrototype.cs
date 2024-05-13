using Robust.Shared.Prototypes;

namespace Content.Shared.Dynamic;

[Prototype("dynamicValue")]
public sealed class DynamicValuePrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public DynamicValue Value = default!;
}