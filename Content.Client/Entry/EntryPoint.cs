using Content.Client.Game;
using Content.Shared.Physics.Components;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Shared.Map;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IContentStyleSheetManager _styleSheetManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public EntityUid MapUid;
    
    public override void PreInit()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        
        IoCManager.Resolve<IBaseClient>().StartSinglePlayer();
        
        MapUid = _entityManager.System<MapSystem>().CreateMap(false);

        var camera = Spawn("camera", new Vector3(0,1,5), EulerAngles.CreateFromDegrees(0,180.0,0));

        _playerManager.SetAttachedEntity(_playerManager.LocalSession, camera);
        _stateManager.RequestStateChange<ContentGameState>();
        
        Spawn("femboy", new Vector3(1,0,0), EulerAngles.CreateFromDegrees(90.0*2,0,0));
        var ent = Spawn("alexandra", new Vector3(0,-1,8), EulerAngles.CreateFromDegrees(0,0,0));
        Spawn("nyash", new Vector3(-1,0,0), EulerAngles.CreateFromDegrees(90.0,0,0));
        
        _entityManager.System<AlexandraAnimationSystem>().Play(ent);
    }
    

    private EntityUid Spawn(string protoId, Vector3 position, EulerAngles rotation)
    {
        var transform = _entityManager.System<Transform3dSystem>();
        
        var ent = _entityManager.Spawn(protoId);
        transform.SetParent(ent, MapUid);
        transform.SetWorldPosition(ent, position);
        transform.SetWorldRotation(ent, rotation);

        return ent;
    }
}