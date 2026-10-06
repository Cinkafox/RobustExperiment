using Content.Shared.Location;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract class SharedGameTicker : EntitySystem
{
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] private readonly LocationSystem _locationSystem = default!;
    
    public void InitializeGame()
    {
        _locationSystem.LoadLocation("default");
    }

    public void AttachSession(ICommonSession session)
    {
        _locationSystem.AttachSession(session);
    }
}