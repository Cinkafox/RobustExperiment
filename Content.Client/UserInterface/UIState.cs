using Robust.Client.UserInterface;

namespace Content.Client.UserInterface;

public abstract class UIState<T> : Robust.Client.State.State where T : UIScreen, new()
{
    [Dependency] protected readonly IUserInterfaceManager UserInterfaceManager = default!;

    protected T UIScreen => (T)UserInterfaceManager.ActiveScreen!;
    
    protected override void Startup()
    {
        UserInterfaceManager.LoadScreen<T>();
        UIStartup();
    }

    protected virtual void UIStartup()
    {
    }

    protected override void Shutdown()
    {
        UserInterfaceManager.UnloadScreen();
        UIShutdown();
    }
    
    protected virtual void UIShutdown()
    {
    }
}