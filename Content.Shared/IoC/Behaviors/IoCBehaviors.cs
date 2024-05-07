namespace Content.Shared.IoC.Behaviors;

public sealed class IoCBehaviors : IIoCBehaviors
{
    public List<(Type interfaceType, Type implementation)> behaviors = new();
    public Dictionary<Type, IoCBehavior> objbeh = new();

    public void AddBehavior((Type interfaceType, Type implementation) b)
    {
        behaviors.Add(b);
    }

    public void Resolve()
    {
        foreach (var (interfaceType, implementation)  in behaviors)
        {
            var obj = IoCManager.ResolveType(interfaceType);
            foreach (var behavior in implementation.GetInterfaces())
            {
                if (!objbeh.TryGetValue(behavior, out var ioCBehavior))
                {
                    ioCBehavior = new IoCBehavior(behavior);
                    objbeh.Add(behavior, ioCBehavior);
                }

                ioCBehavior.Add(obj);
            }
        }
    }

    public void Initialize<T>()
    {
        if (objbeh.TryGetValue(typeof(T), out var value))
        {
            IoCManager.InjectDependencies(value);
            value.Initialize();
        }
    }
}