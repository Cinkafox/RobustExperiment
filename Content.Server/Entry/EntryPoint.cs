using Content.Server.Acz;
using Content.Server.GameTicking;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;

namespace Content.Server.Entry;

public sealed partial class EntryPoint : GameServer
{
    [Dependency] private IStatusHost _host = default!;

    public override void PreInit()
    {
        Dependencies.InjectDependencies(this);
    }

    public override void Init()
    {
        var aczProvider = new ContentMagicAczProvider(Dependencies);
        _host.SetMagicAczProvider(aczProvider);
    }

    public override void PostInit()
    {
        IoCManager.Resolve<IEntityManager>()
            .System<GameTicker>().InitializeGame();
    }
}