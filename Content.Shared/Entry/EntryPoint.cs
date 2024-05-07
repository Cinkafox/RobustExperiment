using Content.Shared.IoC;
using Robust.Shared.ContentPack;

namespace Content.Shared.Entry;

public sealed class EntryPoint : GameShared
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    
    public override void PreInit()
    {
        IoCExt.AutoRegisterWithAttr();
    }

    public override void Init()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
        IoCExt.Behaviors.Initialize<IInitializeBehavior>();
        
        _componentFactory.DoAutoRegistrations();
        _componentFactory.IgnoreMissingComponents();
        _componentFactory.GenerateNetIds();
    }

    public override void PostInit()
    {
        IoCExt.Behaviors.Initialize<IPostInitializeBehavior>();
    }
}