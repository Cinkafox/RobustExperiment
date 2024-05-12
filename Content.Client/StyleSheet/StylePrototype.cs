using Content.Client.StyleSheet.Dynamic;
using Robust.Shared.Prototypes;

namespace Content.Client.StyleSheet;

[Prototype("styleSheet")]
public sealed class StyleSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField] public ProtoId<StyleSheetPrototype>? Parent;
    
    [DataField] public Dictionary<string, Dictionary<string,DynamicValue>> Styles = new();
    [DataField] public Dictionary<string, string> TypeDefinition = new();
}
