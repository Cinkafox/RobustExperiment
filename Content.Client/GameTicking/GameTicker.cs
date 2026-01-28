using Content.Client.Game;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.State;
using Robust.Shared.Timing;

namespace Content.Client.GameTicking;

public sealed class GameTicker : SharedGameTicker
{
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        if (_baseClient.RunLevel == ClientRunLevel.SinglePlayerGame)
        {
            Timer.Spawn(0,StartSinglePlayer);
        }
    }
    
    private void StartSinglePlayer()
    {
        InitializeGame();
        _stateManager.RequestStateChange<ContentGameState>();
    }
}