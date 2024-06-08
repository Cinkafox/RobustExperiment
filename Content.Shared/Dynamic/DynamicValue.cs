namespace Content.Shared.Dynamic;

[DataDefinition, Serializable, Virtual]
public partial class DynamicValue
{
    public static string ReadByPrototypeCommand = "readByPrototype";
    
    [ViewVariables] private string _valueType = ReadByPrototypeCommand;
    private object _value = default!;

    public virtual object GetValueObject()
    {
        if (_value is LazyDynamicValue lazyDynamicValue)
            _value = lazyDynamicValue.Object;
               
        return _value;
    }

    public virtual string GetValueType()
    {
        return _valueType;
    }

    public DynamicValue(string valueType, object value)
    {
        _valueType = valueType;
        _value = value;
    }
}