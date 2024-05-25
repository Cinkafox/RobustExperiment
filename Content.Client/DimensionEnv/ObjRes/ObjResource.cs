using System.IO;
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

        using var stream = manager.ContentFileRead(path);
        using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
        {
            Mesh = Mesh.Parse(reader,path.Directory);
        }
    }
}
