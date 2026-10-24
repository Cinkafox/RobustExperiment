using Content.Shared.GameState;
using Content.Shared.Location;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class SharedGameTicker : EntitySystem
{
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;
    [Dependency] private LocationSystem _locationSystem = default!;
    [Dependency] private SharedContentGameStateSystem _contentGameStateSystem = default!;
    
    public void InitializeGame()
    {
        _locationSystem.LoadLocation("default");
    }

    public void AttachSession(ICommonSession session)
    {
        _locationSystem.AttachSession(session);
        _contentGameStateSystem.ChangeGameState(session, ContentGameState.InGame);
    }
}