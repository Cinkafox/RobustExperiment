using Content.Client.StyleSheet.Dynamic;
using Robust.Shared.Prototypes;

namespace Content.Client.StyleSheet;

[Prototype("styleSheet")]
public sealed class StyleSheetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    
    [DataField] public Dictionary<string, List<StyleProt>> Styles = new();
}

[DataDefinition, Serializable]
public sealed partial class StyleProt
{
    [DataField] public StyleAct Act = StyleAct.Prop;
    [DataField] public DynamicValue Value;
    [DataField] public string Key = string.Empty;
}


public enum StyleAct
{
    Prop,
    Class,
    Pseudo,
    Identifier
}

