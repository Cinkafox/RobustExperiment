using System.Numerics;
using Content.Client.DimensionEnv.ObjRes;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
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
        var mesh = _resourceCache.GetResource<ObjResource>("/Models/alexandra/untitled_back.obj").Mesh;
        mesh.ApplyTransform(Matrix4.CreateTranslation(new Vector3(0,-1f,0)) * Matrix4.Scale(0.3f));
        _sa = new SaObject(mesh, DrawingInstance.AllocTexture(mesh.Materials));
    }
    
    public readonly DrawingInstance DrawingInstance = new();
    
    public Matrix4 CurrentTransform = Matrix4.CreateRotationY(0.002f);
    public Matrix4 MouseTransform = Matrix4.Identity;
    public CameraProperties CameraProperties = new(new Vector3(0, 0, -55), new Angle3d(), 4);

    private bool IsMousePressed;
    
    Vector2 PastPos = Vector2.Zero;
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        var drawHandle = new DrawingHandle3d(handle,Width,Height, CameraProperties,DrawingInstance,_profManager);

        var currPos = UserInterfaceManager.MousePositionScaled.Position;
        var delta = currPos - PastPos;
        PastPos = currPos;

        MouseTransform = Matrix4.CreateRotationY(delta.X / 200);// * Matrix4.CreateRotationX(delta.Y / 200);
        
        _sa.Mesh.ApplyTransform(CurrentTransform * MouseTransform);
        _sa.Draw(drawHandle);
        drawHandle.Flush();
    }
    
}