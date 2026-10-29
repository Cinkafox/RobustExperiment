using Content.Client.Game;
using Content.Client.Game.UI;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems;

[UsedImplicitly]
public sealed partial class RelativeMouseController : UIController, IOnStateEntered<ContentGameState>, IOnStateExited<ContentGameState>
{
    [Dependency] private IInputManager _inputManager = default!;
    
    public override void Initialize()
    {
        _inputManager.SetInputCommand(EngineKeyFunctions.CloseModals, InputCmdHandler.FromDelegate(_ => SetRelativeMode(false)));
    }
    
    public void SetRelativeMode(bool relativeMode)
    {
        if(UIManager.ActiveScreen is not ContentGame screen)
            return;

        screen.MainViewport.EnableMouseRelative = relativeMode;
    }
    
    public void OnStateEntered(ContentGameState state)
    {
        SetRelativeMode(true);
    }

    public void OnStateExited(ContentGameState state)
    {
        SetRelativeMode(false);
    }
}