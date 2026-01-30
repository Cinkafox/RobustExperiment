using System.Diagnostics.CodeAnalysis;
using Content.Client.ConfigurationUI;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Camera;
using Content.Shared.Debug;
using Content.Shared.Transform;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
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
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly ConfigurationUIManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    
    private DebugSystem _debug = default!;

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
        _debug = _entityManager.System<DebugSystem>();
    }

    public readonly DrawingInstance DrawingInstance = new();

    private void DrawSkyBox(DrawingHandleScreen handle)
    {
        if(!TryGetCamera(out var camera)) 
            return;

        SkyInstance.SetParameter("cameraDir", camera.Value.Comp2.WorldAngle.ToVec());
        
        handle.UseShader(SkyInstance);
        handle.DrawRect(PixelRect, Color.White);
        handle.UseShader(null);
    }
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        using (_profManager.Group("3d"))
        {
            Draw3d(handle);
        }
    }

    private bool TryGetCamera([NotNullWhen(true)] out Entity<CameraComponent, Transform3dComponent>? camera)
    {
        camera = null;
        if (!_entityManager.TryGetComponent<CameraComponent>(_playerManager.LocalEntity, out var cameraComponent) ||
            !_entityManager.TryGetComponent<Transform3dComponent>(_playerManager.LocalEntity, out var transform3dComponent))
            return false;

        camera = new Entity<CameraComponent, Transform3dComponent>(_playerManager.LocalEntity.Value, cameraComponent, transform3dComponent);
        return true;
    }

    private void Draw3d(DrawingHandleScreen handle)
    {
        if(_configuration.GetValue<bool>("pause_render"))
            return;
        
        if(!TryGetCamera(out var camera))
           return;
        
        
        if(_configuration.GetValue<bool>("render_shitty_skybox"))
        {
            using (_profManager.Group("DrawSkyBox"))
            {
                DrawSkyBox(handle);
            }
        }
        
        var drawHandle = new DrawingHandle3d(handle, PixelSize.X, PixelSize.Y, camera.Value, DrawingInstance);
        
        if (_configuration.GetValue<bool>("render_debug"))
            drawHandle.DrawDebug = true;
        
        if (!_configuration.GetValue<bool>("render_lighting"))
            drawHandle.DrawLighting = false;

        using (_profManager.Group("DrawAllMeshes"))
        {
            var query = _entityManager.EntityQueryEnumerator<Transform3dComponent, ModelComponent>();
            while (query.MoveNext(out var uid, out var transform3dComponent, out var modelComponent))
            {
                if(camera.Value.Owner == uid) 
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
                    modelComponent.MeshRender.Draw(drawHandle);
                }
            }
        }

        _info.Text = $"                  Triangles: {DrawingInstance.TriangleBuffer.Length}, Textures pool: {DrawingInstance.TextureBuffer.Length}";

        using (_profManager.Group("Flush"))
        {
            drawHandle.Flush(_profManager);
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

        while (_debug.DrawQueue.TryDequeue(out var p))
        {
            drawHandle.DrawCircle(p.Position, p.Radius, p.Color);
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