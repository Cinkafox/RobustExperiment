using Content.Shared.GameState;
using Robust.Client.State;
using Robust.Shared.Player;
using ContentGameState = Content.Client.Game.ContentGameState;

namespace Content.Client.GameState;

public sealed partial class ContentGameStateSystem : SharedContentGameStateSystem
{
    [Dependency] private IStateManager _stateManager = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<GameStateEvent>(OnChangeStateEvent);
    }

    private void OnChangeStateEvent(GameStateEvent ev)
    {
        ChangeGameState(null!, ev.ContentGameState);
    }

    public override void ChangeGameState(ICommonSession session, Shared.GameState.ContentGameState state)
    {
        if (state is Shared.GameState.ContentGameState.InGame)
        {
            _stateManager.RequestStateChange<ContentGameState>();
        }
    }
}