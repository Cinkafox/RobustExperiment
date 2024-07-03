using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking;

public sealed class ServerGameTicker : SharedGameTicker
{
    public override void Initialize()
    {
        PlayerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        Timer.Spawn(0, InitializeMap);
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Timer.Spawn(0, () => PlayerManager.JoinGame(session));
                Logger.Info($"{session.Name} has connected");
                break;
            case SessionStatus.InGame:
                AddSession(session);
                break;
        }
    }
}