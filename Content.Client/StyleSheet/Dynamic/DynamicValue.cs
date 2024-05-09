namespace Content.Client.StyleSheet.Dynamic;

[DataDefinition, Serializable]
public sealed partial class DynamicValue
{
    [DataField] public string ValueType = typeof(string).FullName!;
    public object Value;

    public DynamicValue(string valueType, object value)
    {
        ValueType = valueType;
        Value = value;
    }
}