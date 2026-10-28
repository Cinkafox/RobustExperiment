using System.Numerics;
using Content.Client.Game;
using Content.Client.Game.UI;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems;

[UsedImplicitly]
public sealed partial class RelativeMouseController : UIController, IOnStateEntered<ContentGameState>, IOnStateExited<ContentGameState>
{
    public void SetRelativeMode(bool relativeMode)
    {
        if(UIManager.ActiveScreen is not ContentGame screen)
            return;

        screen.MainViewport.RelativeMode = relativeMode;
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