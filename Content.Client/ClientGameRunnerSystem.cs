using Content.Shared;
using Robust.Shared.Map;

namespace Content.Client;

public sealed class ClientGameRunnerSystem : SharedGameRunnerSystem
{
    public void StartSinglePlayer()
    {
        InitializeMap();
        AddSession(_playerManager.LocalSession!);
    }
}