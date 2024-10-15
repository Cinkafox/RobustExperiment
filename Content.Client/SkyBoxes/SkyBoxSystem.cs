using System.Numerics;
using Content.Client.DimensionEnv.ObjRes;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Robust.Client.Graphics;
using Robust.Client.Utility;

namespace Content.Client.SkyBoxes;

public sealed class SkyBoxSystem: EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SkyBoxComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<SkyBoxComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Texture = ent.Comp.SkyboxPath.Frame0();
    }
}