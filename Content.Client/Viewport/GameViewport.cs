﻿using System.Numerics;
using Content.Client.Camera;
using Content.Client.ConfigurationUI;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Client.SkyBoxes;
using Content.Shared.Transform;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Profiling;
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

    private Label Info;
    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        Info = new Label();
        _userInterfaceManager.OnScreenChanged += UserInterfaceManagerOnOnScreenChanged;
        
    }

    private void UserInterfaceManagerOnOnScreenChanged((UIScreen? Old, UIScreen? New) obj)
    {
        obj.New?.AddChild(Info);
    }

    public readonly DrawingInstance DrawingInstance = new();

    private void DrawSkyBox(DrawingHandleScreen handle)
    {
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue || 
           !_entityManager.TryGetComponent<SkyBoxComponent>(_cameraManager.Camera!.Value.uid, out var skyBoxComponent))
        {
            return;
        };

        var rot = (float)(cameraProp.Value.Angle.Yaw.Theta / float.Pi);

        var scale = skyBoxComponent.Texture.Size;
        var shift = new Vector2(rot, 0);
        var lefttop = (new Vector2(0 / 4f, 1 / 3f)) * scale;
        var rightbottom = (new Vector2(4 / 4f, 2 / 3f)) * scale;
        
        handle.DrawTextureRectRegion(skyBoxComponent.Texture, new UIBox2(-shift * scale, (Rect.BottomRight*new Vector2(2,1) - shift* scale)), new UIBox2(lefttop , rightbottom));

        if (rot < 0)
        {
            handle.DrawTextureRectRegion(skyBoxComponent.Texture, new UIBox2(-shift * scale - new Vector2(Rect.Right ,0), (Rect.BottomRight*new Vector2(2,1) - shift* scale - new Vector2(Rect.Right ,0))), new UIBox2(lefttop , rightbottom));
        }
    }
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        if(_configuration.GetValueOrDefault<bool>("pause_render", false))
            return;
        
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue)
        {
            return;
        };
        
        DrawSkyBox(handle);
        
        var drawHandle = new DrawingHandle3d(handle, Width, Height, cameraProp.Value, DrawingInstance,_profManager,_parallel);
        
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
            
            var gr5 = _profManager.Group("Draw3d.DrawMesh");
            modelComponent.MeshRender.Draw(drawHandle);
            gr5.Dispose();
        }

        Info.Text = $"                  Triangles: {DrawingInstance.TriangleBuffer.Count}, Textures pool: {DrawingInstance.TextureBuffer.Length}";
        
        drawHandle.Flush();
        
        if(!_configuration.GetValueOrDefault<bool>("transform_view_enabled", false))
            return;
        
        var q = _entityManager.EntityQueryEnumerator<Transform3dComponent>();
        while (q.MoveNext(out var t))
        {
            drawHandle.DrawCircle(t.WorldPosition, 20f, Color.Aqua);
            var d = t.WorldAngle.ToVec() * 0.2f + t.WorldPosition;
            drawHandle.DrawCircle(d, 15f, Color.Blue);
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