using Content.Client.MainMenu;
using Robust.Client.State;
using Robust.Shared.ContentPack;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IStateManager _stateManager = default!;

    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void PostInit()
    {
        _stateManager.RequestStateChange<MenuState>();
    }
}