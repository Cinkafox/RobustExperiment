using Content.Client.Input;
using Content.Client.MainMenu;
using Content.Client.StyleSheet;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly StyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
       ContentContexts.SetupContexts(_inputManager.Contexts);
       _userInterfaceManager.SetDefaultTheme("DefaultTheme");
       _styleSheetManager.ApplyStyleSheet("default");
       _userInterfaceManager.MainViewport.Visible = false;
       _stateManager.RequestStateChange<MenuState>();
    }
}