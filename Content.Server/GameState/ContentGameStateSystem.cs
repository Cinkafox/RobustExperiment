using Content.Shared.GameState;
using Robust.Shared.Player;

namespace Content.Server.GameState;

public sealed partial class ContentGameStateSystem : SharedContentGameStateSystem
{
    public override void ChangeGameState(ICommonSession session, Shared.GameState.ContentGameState state)
    {
        RaiseNetworkEvent(new GameStateEvent(state), session);
    }
}