using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.ConfigurationUI;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Camera;
using Content.Shared.Physics;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;

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

    private readonly Label _info;
    private readonly ShaderInstance _skyInstance;
    
    public readonly DrawingInstance DrawingInstance;
    
    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        DrawingInstance = new DrawingInstance(_prototypeManager);
        
        _info = new Label();
        _userInterfaceManager.OnScreenChanged += UserInterfaceManagerOnOnScreenChanged;
        _skyInstance = _prototypeManager.Index<ShaderPrototype>("SkyShader").InstanceUnique();
    }

    private void UserInterfaceManagerOnOnScreenChanged((UIScreen? Old, UIScreen? New) obj)
    {
        obj.New?.AddChild(_info);
    }
    
    private void DrawSkyBox(DrawingHandleScreen handle)
    {
        if(!TryGetCamera(out var camera)) 
            return;

        _skyInstance.SetParameter("cameraDir", camera.Value.Comp2.WorldAngle.ToVec());
        
        handle.UseShader(_skyInstance);
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
        
        _info.Text = $"                  Triangles: {DrawingInstance.GetDrawnTriangles()}, Textures pool: {DrawingInstance.TextureBuffer.Length}";
        
        using (_profManager.Group("Flush"))
        {
            drawHandle.Flush(_profManager);
        }
        
        if(_configuration.GetValue<bool>("transform_view_enabled"))
        {
            using (_profManager.Group("DrawTransform"))
            {
                var q = _entityManager.EntityQueryEnumerator<Transform3dComponent, RigidBodyComponent>();
                while (q.MoveNext(out var transform3d, out var rigidBodyComponent))
                {
                    var transformedPhysics = new TransformedPhysicShape(transform3d, rigidBodyComponent.Shape);
                    var debugHandler = new DebugDrawingHandle();
                    rigidBodyComponent.Shape.DrawShape(debugHandler, transformedPhysics);
                
                    foreach (var (radius, position) in debugHandler.SphereBuffer)
                    {
                        drawHandle.DrawCircle(position, radius, Color.Aqua);
                    }

                    foreach (var vertexes in debugHandler.VertexBuffer)
                    {
                        drawHandle.DrawDebugFace(vertexes);
                    }
                }
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