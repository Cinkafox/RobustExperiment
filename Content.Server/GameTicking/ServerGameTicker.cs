using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking;

public sealed class ServerGameTicker : SharedGameTicker
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        Timer.Spawn(0, InitializeMap);
    }

    public override void ChangeSessionState<T>(ICommonSession session)
    {
        RaiseNetworkEvent(new SessionStateChangeRequiredEvent(typeof(T)),session);
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Timer.Spawn(0, () => _playerManager.JoinGame(session));
                Logger.Info($"{session.Name} was connected");
                break;
            case SessionStatus.InGame:
                AddSession(session);
                break;
        }
    }
}