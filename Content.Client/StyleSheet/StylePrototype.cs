using Robust.Shared.Prototypes;
using DynamicValue = Content.Shared.Dynamic.DynamicValue;

namespace Content.Client.StyleSheet;

[Prototype("styleSheet")]
public sealed class StyleSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public List<ProtoId<StyleSheetPrototype>> Parents = new();
    
    [DataField] public Dictionary<string, Dictionary<string,DynamicValue>> Styles = new();
    [DataField] public Dictionary<string, string> TypeDefinition = new();
}
