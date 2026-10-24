using System.Globalization;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Shared.Entry;

public sealed partial class EntryPoint : GameShared
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ILocalizationManager _localizationManager = default!;
    [Dependency] private IConfigurationManager _configurationManager = default!;
    
    private const string Culture = "ru-RU";
    
    public override void PreInit()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
        
        _localizationManager.LoadCulture(new CultureInfo(Culture));
    }

    public override void Init()
    {
        _componentFactory.DoAutoRegistrations();
        _componentFactory.IgnoreMissingComponents();
        _componentFactory.GenerateNetIds();
    }

    public override void PostInit()
    {
        _configurationManager.SetCVar("net.pvs", false); //TODO: PVS for 3d think
    }
}