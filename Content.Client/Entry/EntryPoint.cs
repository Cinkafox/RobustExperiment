using Content.Shared.Input;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.Input;

namespace Content.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IContentStyleSheetManager _styleSheetManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    
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