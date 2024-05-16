using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.IoC.Behaviors;

public sealed class IoCBehavior
{
    private readonly List<object> _behaviors = new();
    private readonly Type _interface;

    public IoCBehavior(Type @interface)
    {
        _interface = @interface;
    }

    public void Add(object beh)
    {
        _behaviors.Add(beh);
    }

    public bool Check<T>(object behavior,[NotNullWhen(true)] out T? beh)
    {
        if (behavior is T b && _interface == typeof(T))
        {
            beh = b;
            return true;
        }
        beh = default;
        return false;
    }

    public void Initialize()
    {
        foreach (var behavior in _behaviors)
        {
            IoCManager.InjectDependencies(behavior);
            
            if (Check<IInitializeBehavior>(behavior,out var initializeBehavior))
            {
                initializeBehavior.Initialize();
            }

            if (Check<IPostInitializeBehavior>(behavior,out var postInitializeBehavior))
            {
                postInitializeBehavior.PostInitialize();
            }
        }
    }
}