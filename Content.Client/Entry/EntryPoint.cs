using Content.Client.Game;
using Content.Client.StyleSheet;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly StyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        
        IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
        
        var mapId = _mapManager.CreateMap();
        var transform = _entityManager.System<Transform3dSystem>();

        var camera = _entityManager.Spawn("camera");
        transform.AttachTo(camera, mapId);
        transform.SetPosition(camera, new Vector3(0, 1, -5));

        _playerManager.SetAttachedEntity(_playerManager.LocalSession, camera);
        
        var ent = _entityManager.Spawn("alexandra");
        transform.AttachTo(ent, mapId);
        transform.SetPosition(ent, new Vector3(2,0,0));
        transform.SetRotation(ent, new Angle3d(0, Angle.FromDegrees(180), 0));
        
        var ent1 = _entityManager.Spawn("car");
        transform.AttachTo(ent1, mapId);
        transform.SetPosition(ent1, new Vector3(-2,0,0));
        transform.SetRotation(ent1, new Angle3d(0, Angle.FromDegrees(195), 0));
        
        _stateManager.RequestStateChange<ContentGameState>();
    }
}