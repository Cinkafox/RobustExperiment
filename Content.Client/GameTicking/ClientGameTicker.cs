using Content.Shared.GameTicking;

namespace Content.Client.GameTicking;

public sealed class ClientGameTicker : SharedGameTicker
{
    public void StartSinglePlayer()
    {
        InitializeMap();
        AddSession(PlayerManager.LocalSession!);
    }
}