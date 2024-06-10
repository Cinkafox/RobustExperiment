namespace Content.Shared.Dynamic;

public sealed class LazyDynamicValue
{
    private readonly Func<object> _createObject;
    private object? _object;
    public object Object
    {
        get
        {
            if (_object is null)
                _object = _createObject.Invoke();
            
            return _object;
        }
    }

    public LazyDynamicValue(Func<object> createObject)
    {
        _createObject = createObject;
    }
}
