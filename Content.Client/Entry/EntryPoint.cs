using Content.Client.Game;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IStyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    
    public override void PreInit()
    {
        StyleSheetify.Client.DependencyRegistration.Register(IoCManager.Instance!);
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        
        IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
        
        var mapId = _entityManager.System<MapSystem>().CreateMap();
        var transform = _entityManager.System<Transform3dSystem>();

        var camera = _entityManager.Spawn("camera");
        transform.SetParent(camera, mapId);
        transform.SetWorldPosition(camera, new Vector3(0, 2, 5));
        transform.SetWorldRotation(camera, new EulerAngles(0,Angle.FromDegrees(180),0));

        _playerManager.SetAttachedEntity(_playerManager.LocalSession, camera);
        
        var ent = _entityManager.Spawn("alexandra");
        transform.SetParent(ent, mapId);
        transform.SetWorldPosition(ent, new Vector3(0,0,0));
        transform.SetWorldRotation(ent, new EulerAngles(0,Angle.FromDegrees(0),0));
        
        _stateManager.RequestStateChange<ContentGameState>();
        
        _entityManager.System<AlexandraAnimationSystem>().Play(ent);
    }
}