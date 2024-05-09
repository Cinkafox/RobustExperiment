using System.Globalization;
using Content.Shared.IoC;
using Robust.Shared.ContentPack;

namespace Content.Shared.Entry;

public sealed class EntryPoint : GameShared
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    
    private const string Culture = "ru-RU";
    
    public override void PreInit()
    {
        IoCExt.AutoRegisterWithAttr();
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
        IoCExt.Behaviors.Resolve();
        
        _localizationManager.LoadCulture(new CultureInfo(Culture));
    }

    public override void Init()
    {
        _componentFactory.DoAutoRegistrations();
        _componentFactory.IgnoreMissingComponents();
        _componentFactory.GenerateNetIds();
        
        IoCExt.Behaviors.Initialize<IInitializeBehavior>();
    }

    public override void PostInit()
    {
        IoCExt.Behaviors.Initialize<IPostInitializeBehavior>();
    }
}