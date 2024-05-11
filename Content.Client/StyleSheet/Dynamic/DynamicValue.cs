namespace Content.Client.StyleSheet.Dynamic;

[DataDefinition, Serializable, Virtual]
public partial class DynamicValue
{
    public static string ReadByPrototypeCommand = "readByPrototype";
    
    [DataField] public string ValueType = ReadByPrototypeCommand;
    public object Value;

    public DynamicValue(string valueType, object value)
    {
        ValueType = valueType;
        Value = value;
    }
}