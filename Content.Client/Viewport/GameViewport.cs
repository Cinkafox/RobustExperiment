﻿using Content.Client.Bone;
using Content.Client.Camera;
using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Profiling;
using Robust.Shared.Threading;
using Vector3 = Robust.Shared.Maths.Vector3;

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
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        var cameraProp = _cameraManager.CameraProperties;
        if(!cameraProp.HasValue)
        {
            return;
        };

        var gr1 = _profManager.Group("Draw3d.HandleInitialize");
        var drawHandle = new DrawingHandle3d(handle, Width, Height, cameraProp.Value, DrawingInstance,_profManager,_parallel);
        gr1.Dispose();
        
        
        var gr2 = _profManager.Group("Draw3d.DrawingQuery");
        var query = _entityManager.EntityQueryEnumerator<Transform3dComponent, ModelComponent>();
        while (query.MoveNext(out var transform3dComponent, out var modelComponent))
        {
            if (!modelComponent.MeshRenderInitialized)
            {
                var gr4 = _profManager.Group("Draw3d.DrawMeshInit");
                modelComponent.MeshRender = new MeshRender(modelComponent.CurrentMesh,
                    DrawingInstance.AllocTexture(modelComponent.CurrentMesh.Materials));
                modelComponent.MeshRenderInitialized = true;
                gr4.Dispose();
            }

            modelComponent.MeshRender.Transform = transform3dComponent.WorldMatrix;
            
            var gr5 = _profManager.Group("Draw3d.DrawMesh");
            modelComponent.MeshRender.Draw(drawHandle);
            gr5.Dispose();
        }
        gr2.Dispose();

        Info.Text = $"                  Triangles: {DrawingInstance.TriangleBuffer.Count}, Textures pool: {DrawingInstance.TextureBuffer.Length}";
        
        var gr3 = _profManager.Group("Draw3d.Flush");
        drawHandle.Flush();
        gr3.Dispose();
        
        // var q = _entityManager.EntityQueryEnumerator<Transform3dComponent, BoneComponent>();
        // while (q.MoveNext(out var t, out var b))
        // {
        //     drawHandle.DrawCircle(t.WorldPosition, 8f, Color.Aqua, true);
        // }
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