using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client.MainMenu;

public sealed class MenuState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    
    protected override void Startup()
    {
        _userInterfaceManager.LoadScreen<UI.MainMenu>();
    }

    protected override void Shutdown()
    {
        _userInterfaceManager.UnloadScreen();
    }
}