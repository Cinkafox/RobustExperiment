using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Utils;

// Fuck you, sandboxviolation!
public sealed class RobustLazy<T>
{
    [field: AllowNull, MaybeNull]
    public T Value
    {
        get
        {
            if (field == null)
                field = _factory();
            
            return field;
        }
    }

    private readonly Func<T> _factory;

    public RobustLazy(Func<T> factory)
    {
        _factory = factory;
    }
}