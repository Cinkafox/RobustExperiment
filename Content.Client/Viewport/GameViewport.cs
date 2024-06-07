using Content.Client.DimensionEnv.ObjRes;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Profiling;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Profiling;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public sealed class GameViewport : Control
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private SaObject _sa;
    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        var mesh = _resourceCache.GetResource<ObjResource>("/Models/tnew/tardis_2010.obj").Mesh;
        _sa = new SaObject(mesh, DrawingInstance.AllocTexture(mesh.Materials));
    }
    
    public readonly DrawingInstance DrawingInstance = new();
    
    public Matrix4 CurrentTransform = Matrix4.CreateRotationX(0.003f) * Matrix4.CreateRotationY(0.001f) * Matrix4.CreateRotationZ(0.002f);
    public CameraProperties CameraProperties = new(new Vector3(0, 0, -30), new Angle3d(), 4);
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        var drawHandle = new DrawingHandle3d(handle,Width,Height, CameraProperties,DrawingInstance,_profManager);

        _sa.Mesh.ApplyTransform(CurrentTransform);
        _sa.Draw(drawHandle);
        drawHandle.Flush();
    }
}