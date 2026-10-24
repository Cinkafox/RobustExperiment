using System.Threading;
using System.Threading.Tasks;
using Content.Packaging;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Server.ServerStatus;

namespace Content.Server.Acz;

public sealed class ContentMagicAczProvider(IDependencyCollection deps) : IMagicAczProvider
{
    public async Task Package(AssetPass pass, IPackageLogger logger, CancellationToken cancel)
    {
        var contentDir = DefaultMagicAczProvider.FindContentRootPath(deps);

        await ClientPackaging.WriteResources(contentDir, pass, logger, cancel);
    }
}