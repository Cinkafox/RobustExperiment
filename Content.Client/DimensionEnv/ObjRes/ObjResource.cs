using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class ObjResource : BaseResource
{
    public Mesh Mesh = default!;
    
    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var manager = dependencies.Resolve<IResourceManager>();

        using var reader = manager.ContentFileReadText(path);
        Mesh = Mesh.Parse(dependencies, reader, path.Directory);
    }
}
