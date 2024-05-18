using Content.Client.DimensionEnv.ObjRes;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Viewport;

public sealed class GameViewport : Control
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    private Texture _texture;
    private Mesh _mesh;
    private SaObject _sa;

    public GameViewport()
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
        
        _texture = _resourceCache.GetResource<TextureResource>("/Textures/fat-gorilla.png").Texture;
        _mesh = _resourceCache.GetResource<ObjResource>("/Models/teapot/teapot.obj").Mesh;
        _sa = new SaObject(_mesh, _texture);
        _inputManager.SetInputCommand(EngineKeyFunctions.MoveUp, new CameraMoverInputHandler(() =>
        {
            CameraProperties.Angle += new Angle3d(0, 0.1, 0);
            Logger.Debug(CameraProperties.CameraDirection + "<<");
        }));
        
        _inputManager.SetInputCommand(EngineKeyFunctions.MoveDown, new CameraMoverInputHandler(() =>
        {
            CameraProperties.Angle += new Angle3d(0, -0.1, 0);
            Logger.Debug(CameraProperties.CameraDirection + "<<");
        }));
        
        _inputManager.SetInputCommand(EngineKeyFunctions.MoveLeft, new CameraMoverInputHandler(() =>
        {
            CameraProperties.Angle += new Angle3d(-0.1, 0, 0);
            Logger.Debug(CameraProperties.CameraDirection + "<<");
        }));
        
        _inputManager.SetInputCommand(EngineKeyFunctions.MoveRight, new CameraMoverInputHandler(() =>
        {
            CameraProperties.Angle += new Angle3d(0.1, 0, 0);
            Logger.Debug(CameraProperties.CameraDirection + "<<");
        }));
        
        _inputManager.SetInputCommand(EngineKeyFunctions.Use, new CameraMoverInputHandler(() =>
        {
            //CameraProperties.Position += CameraProperties.CameraDirection * -3;
        }));
    }

    public DrawingInstance DrawingInstance = new DrawingInstance();
    public Matrix4 CurrentTransform = Matrix4.CreateRotationX(0.003f) * Matrix4.CreateRotationY(0.001f) * Matrix4.CreateRotationZ(0.002f);
    public CameraProperties CameraProperties = new CameraProperties(new Vector3(0, 0, 8), new Angle3d(), 4);

    protected override void Draw(DrawingHandleScreen handle)
    {
        var drawHandle = new DrawingHandle3d(handle,Width,Height, CameraProperties,DrawingInstance);

        _sa.Mesh.ApplyTransform(CurrentTransform);
        _sa.Draw(drawHandle);
        
        drawHandle.Flush();
    }
}

public sealed class CameraMoverInputHandler : InputCmdHandler
{
    private readonly Action _action;
    public override bool FireOutsidePrediction { get; } = true;

    public override void Enabled(ICommonSession? session)
    {
        _action();
    }

    public CameraMoverInputHandler(Action act)
    {
        _action = act;
    }
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        return true;
    }
}