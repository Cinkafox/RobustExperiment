using Content.Shared.Input;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.Input;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IContentStyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    
    public override void PreInit()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        ContentContexts.SetupContexts(_inputManager.Contexts);
        
        IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
    }
}