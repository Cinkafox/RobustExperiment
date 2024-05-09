using Content.Client.MainMenu;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Init()
    {
        
    }

    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _stateManager.RequestStateChange<MenuState>();
    }
}