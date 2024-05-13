using Content.Client.UI;

namespace Content.Client.MainMenu;
public sealed class MenuState : UIState<UI.MainMenu>
{
    protected override void UIStartup()
    {
        Logger.Debug("Hello from menu");
    }
}