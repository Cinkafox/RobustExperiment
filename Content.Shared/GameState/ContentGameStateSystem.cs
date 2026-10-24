using Robust.Shared.Player;

namespace Content.Shared.GameState;

public abstract class SharedContentGameStateSystem : EntitySystem
{
    public abstract void ChangeGameState(ICommonSession session, ContentGameState state);
}