using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared;

public abstract class SharedGameRunnerSystem : EntitySystem
{
    [Dependency] protected readonly IMapManager _mapManager = default!;
    [Dependency] protected readonly SharedMapSystem _mapSystem = default!;
    [Dependency] protected readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] protected readonly SharedPhysicsSystem _physicsSystem = default!;

    public EntityUid MapUid;
    public MapId MapId;
    public void InitializeMap()
    {
        MapUid = _mapSystem.CreateMap(out MapId);
        var mapEntity = _mapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        _mapSystem.SetTile(mapEntity, new Vector2i(0,0),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(mapEntity, new Vector2i(-1,-1),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(mapEntity, new Vector2i(0,-1),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(mapEntity, new Vector2i(-1,0),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetAmbientLight(MapId, Color.White);
    }

    public void AddSession(ICommonSession session)
    {
        var cam = Spawn("cat", new MapCoordinates(10, 0, MapId));
        _playerManager.SetAttachedEntity(session, cam);
    }
}