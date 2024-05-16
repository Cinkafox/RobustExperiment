namespace Content.Shared.IoC.Behaviors;

public sealed class IoCBehaviors : IIoCBehaviors
{
    public readonly List<(Type interfaceType, Type implementation)> Behaviors = new();
    public readonly Dictionary<Type, IoCBehavior> ResolvedBehaviors = new();

    public void AddBehavior((Type interfaceType, Type implementation) b)
    {
        Behaviors.Add(b);
    }

    public void Resolve()
    {
        foreach (var (interfaceType, implementation) in Behaviors)
        {
            var obj = IoCManager.ResolveType(interfaceType);
            foreach (var behavior in implementation.GetInterfaces())
            {
                if (!ResolvedBehaviors.TryGetValue(behavior, out var ioCBehavior))
                {
                    ioCBehavior = new IoCBehavior(behavior);
                    ResolvedBehaviors.Add(behavior, ioCBehavior);
                }
                
                ioCBehavior.Add(obj);
            }
        }
    }

    public void Initialize<T>()
    {
        if (ResolvedBehaviors.TryGetValue(typeof(T), out var value))
        {
            value.Initialize();
        }
    }
}