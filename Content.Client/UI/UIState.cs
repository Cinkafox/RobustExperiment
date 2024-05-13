using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client.UI;

public abstract class UIState<T> : State where T : UIScreen, new()
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    
    protected override void Startup()
    {
        _userInterfaceManager.LoadScreen<T>();
        UIStartup();
    }

    protected virtual void UIStartup()
    {
    }

    protected override void Shutdown()
    {
        _userInterfaceManager.UnloadScreen();
        UIShutdown();
    }
    
    protected virtual void UIShutdown()
    {
    }
}