using Content.Client.DimensionEnv.ObjRes;

namespace Content.Client.DimensionEnv;

[RegisterComponent]
public sealed partial class ModelComponent : Component
{
    [DataField("path")] public string ObjPath;
    public Mesh CurrentMesh;
    public MeshRender MeshRender;
    public bool MeshRenderInitialized;
}