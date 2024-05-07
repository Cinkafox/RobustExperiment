using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.IoC.Behaviors;

public sealed class IoCBehavior
{
    private static List<object> Behaviors = new();
    public Type Interface;

    public IoCBehavior(Type @interface)
    {
        Interface = @interface;
    }

    public void Add(object beh)
    {
        Behaviors.Add(beh);
    }

    public bool Check<T>(object behavior,[NotNullWhen(true)] out T? beh)
    {
        if (behavior is T b && Interface == typeof(T))
        {
            beh = b;
            return true;
        }
        beh = default;
        return false;
    }

    public void Initialize()
    {
        Logger.Debug(Behaviors.Count + "<<");
        foreach (var behavior in Behaviors)
        {
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