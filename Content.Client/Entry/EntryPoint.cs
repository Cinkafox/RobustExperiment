using Content.Client.MainMenu;
using Content.Client.StyleSheet;
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
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplySheet("default");
        _stateManager.RequestStateChange<MenuState>();
    }
}