using Content.Server.Acz;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.Entry;

public sealed class EntryPoint : GameServer
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
        _prototypeManager.RegisterIgnore("styleSheet");
        _prototypeManager.RegisterIgnore("dynamicValue");
    }

    public override void Init()
    {
        var aczProvider = new ContentMagicAczProvider(IoCManager.Resolve<IDependencyCollection>());
        IoCManager.Resolve<IStatusHost>().SetMagicAczProvider(aczProvider);
    }
}