using Content.Client.Game;
using Content.Client.Game.UI;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems;

[UsedImplicitly]
public sealed partial class ChatUIController : UIController
{
    [Dependency] private IInputManager _inputManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _inputManager.SetInputCommand(ContentKeyFunctions.ChatFocusAction, InputCmdHandler.FromDelegate(OpenChat));
    }

    private void OpenChat(ICommonSession? session)
    {
        if (UIManager.ActiveScreen is ContentGame screen)
        {
            screen.OpenChat();
        }
    }
}