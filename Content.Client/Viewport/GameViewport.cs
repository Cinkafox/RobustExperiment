using Content.Client.Camera;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Profiling;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public sealed class GameViewport : Control
{
    [Dependency] private readonly ProfManager _profManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CameraManager _cameraManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
        
    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
    }
    
    public readonly DrawingInstance DrawingInstance = new();
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue)
        {
            return;
        };
        
        var drawHandle = new DrawingHandle3d(handle, Width, Height, cameraProp.Value, DrawingInstance,_profManager);

        var query = _entityManager.EntityQueryEnumerator<Transform3dComponent, ModelComponent>();
        while (query.MoveNext(out var transform3dComponent, out var modelComponent))
        {
            if (!modelComponent.MeshRenderInitialized)
            {
                modelComponent.MeshRender = new MeshRender(modelComponent.CurrentMesh,
                    DrawingInstance.AllocTexture(modelComponent.CurrentMesh.Materials));
                modelComponent.MeshRenderInitialized = true;
            }

            modelComponent.MeshRender.Mesh.Transform = Matrix4Helpers.CreateTransform(transform3dComponent.LocalPosition, transform3dComponent.LocalRotation, Vector3.One);
            
            modelComponent.MeshRender.Draw(drawHandle);
        }
        
        drawHandle.Flush();
    }
    
    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Handled)
            return;

        _inputManager.ViewportKeyEvent(this, args);
    }

    protected  override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Handled)
            return;

        _inputManager.ViewportKeyEvent(this, args);
    }
    
}