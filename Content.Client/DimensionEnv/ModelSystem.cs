using Content.Client.DimensionEnv.ObjRes;
using Robust.Client.ResourceManagement;

namespace Content.Client.DimensionEnv;

public sealed class ModelSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ModelComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ModelComponent> ent, ref ComponentInit args)
    {
        if (!_resourceCache.TryGetResource<ObjResource>(ent.Comp.ObjPath, out var mesh))
        {
            RemComp(ent, ent.Comp);
            return;
        }

        ent.Comp.CurrentMesh = mesh.Mesh;
    }
}