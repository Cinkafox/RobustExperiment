using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IContentStyleSheetManager _styleSheetManager = default!;
    
    public override void PreInit()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        
        IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
    }
}