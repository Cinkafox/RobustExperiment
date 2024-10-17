using Content.Client.Camera;
using Content.Client.ConfigurationUI;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Client.Utils;
using Content.Shared.Transform;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Threading;

namespace Content.Client.Viewport;

public sealed class GameViewport : Control
{
    [Dependency] private readonly ProfManager _profManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CameraManager _cameraManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly ConfigurationUIManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Label _info;
    private readonly ShaderInstance SkyInstance;
    
    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        _info = new Label();
        _userInterfaceManager.OnScreenChanged += UserInterfaceManagerOnOnScreenChanged;
        SkyInstance = _prototypeManager.Index<ShaderPrototype>("SkyShader").InstanceUnique();
    }

    private void UserInterfaceManagerOnOnScreenChanged((UIScreen? Old, UIScreen? New) obj)
    {
        obj.New?.AddChild(_info);
    }

    public readonly DrawingInstance DrawingInstance = new();

    private void DrawSkyBox(DrawingHandleScreen handle)
    {
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue)
        {
            return;
        };

        SkyInstance.SetParameter("cameraDir", cameraProp.Value.Angle.ToVec().ToRobustVector());
        
        handle.UseShader(SkyInstance);
        handle.DrawRect(Rect, Color.White);
        handle.UseShader(null);
    }
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        using (_profManager.Group("3d"))
        {
            Draw3d(handle);
        }
    }

    private void Draw3d(DrawingHandleScreen handle)
    {
        if(_configuration.GetValue<bool>("pause_render"))
            return;
    
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue)
        {
            return;
        }
        
        if(_configuration.GetValue<bool>("render_shitty_skybox"))
        {
            using (_profManager.Group("DrawSkyBox"))
            {
                DrawSkyBox(handle);
            }
        }
        
        var drawHandle = new DrawingHandle3d(handle, Width, Height, cameraProp.Value, DrawingInstance,_parallel);

        if (_configuration.GetValue<bool>("render_parallel_triangle"))
            drawHandle.DoParallelTriangle = true;
        
        if (_configuration.GetValue<bool>("render_debug"))
            drawHandle.DrawDebug = true;
        
        if (!_configuration.GetValue<bool>("render_lighting"))
            drawHandle.DrawLighting = false;

        using (_profManager.Group("DrawAllMeshes"))
        {
            var query = _entityManager.EntityQueryEnumerator<Transform3dComponent, ModelComponent>();
            while (query.MoveNext(out var uid, out var transform3dComponent, out var modelComponent))
            {
                if(_cameraManager.Camera!.Value.Item3 == uid) 
                    continue;
            
                if (!modelComponent.MeshRenderInitialized)
                {
                    modelComponent.MeshRender = new MeshRender(modelComponent.CurrentMesh,
                        DrawingInstance.AllocTexture(modelComponent.CurrentMesh.Materials));
                    modelComponent.MeshRenderInitialized = true;
                }

                modelComponent.MeshRender.Transform = transform3dComponent.WorldMatrix;

                using (_profManager.Group("DrawMesh_"+uid))
                {
                    if(_configuration.GetValue<bool>("render_draw_parallel")) 
                        modelComponent.MeshRender.DrawParallel(drawHandle);
                    else
                        modelComponent.MeshRender.Draw(drawHandle);
                }
            }
        }

        _info.Text = $"                  Triangles: {DrawingInstance.TriangleBuffer.Count}, Textures pool: {DrawingInstance.TextureBuffer.Length}";

        using (_profManager.Group("Flush"))
        {
            drawHandle.Flush();
        }
        
        if(!_configuration.GetValue<bool>("transform_view_enabled"))
            return;

        using (_profManager.Group("DrawTransform"))
        {
            var q = _entityManager.EntityQueryEnumerator<Transform3dComponent>();
            while (q.MoveNext(out var t))
            {
                drawHandle.DrawCircle(t.WorldPosition, 20f, Color.Aqua);
                var d = t.WorldAngle.ToVec() * 0.2f + t.WorldPosition;
                drawHandle.DrawCircle(d, 15f, Color.Blue);
            }
        }
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