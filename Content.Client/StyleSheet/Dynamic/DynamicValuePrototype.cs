using Robust.Shared.Prototypes;

namespace Content.Client.StyleSheet.Dynamic;

[Prototype("dynamicValue")]
public sealed class DynamicValuePrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public DynamicValue Value = default!;
}