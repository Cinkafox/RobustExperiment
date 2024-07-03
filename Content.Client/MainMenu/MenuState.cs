using Content.Client.UserInterface;
using Content.Shared.States;

namespace Content.Client.MainMenu;
public sealed class MenuState : UIState<UI.MainMenu>, IMenuState
{
    protected override void UIStartup()
    {
        Logger.Debug("Hello from menu");
    }
}