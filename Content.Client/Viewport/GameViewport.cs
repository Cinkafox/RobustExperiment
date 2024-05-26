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
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly ProfManager _profManager = default!;
    
    private Texture _texture;
    private Mesh _mesh;
    private SaObject _sa;

    private Angle3d CamDicTo;
    private int CamPosTo;

    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        
        _texture = _resourceCache.GetResource<TextureResource>("/Models/tnew/79797c43.png").Texture;
        _mesh = _resourceCache.GetResource<ObjResource>("/Models/tnew/tardis_2010.obj").Mesh;
        _sa = new SaObject(_mesh, DrawingInstance.AddTexture(_texture));
    }
    
    public readonly DrawingInstance DrawingInstance = new DrawingInstance();
    
    public Matrix4 CurrentTransform = Matrix4.CreateRotationX(0.001f) * Matrix4.CreateRotationY(0.001f) * Matrix4.CreateRotationZ(0.002f);
    public CameraProperties CameraProperties = new CameraProperties(new Vector3(0, 0, -30), new Angle3d(), 4);

    private int drawCount;

    private void Update()
    {
        Logger.Debug($"TRIANGLES:{DrawingInstance.TriangleBuffer.Length} OF {DrawingInstance.TriangleBuffer.Limit}");
    }
    
    protected override void Draw(DrawingHandleScreen handle)
    {
        drawCount = (drawCount + 1) % 16;
        
        //CameraProperties.Angle += CamDicTo;
        //CameraProperties.Position += CameraProperties.CameraDirection * CamPosTo * 0.5f;
        
        var drawHandle = new DrawingHandle3d(handle,Width,Height, CameraProperties,DrawingInstance,_profManager);

        _sa.Mesh.ApplyTransform(CurrentTransform);
        _sa.Draw(drawHandle);
        
        if(drawCount == 0) Update();
        drawHandle.Flush();
    }
}