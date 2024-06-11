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
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;

    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    public MapId MapId;
    public void InitializeMap()
    {
        MapUid = _mapSystem.CreateMap(out MapId);
        GridUid = _mapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        _mapSystem.SetTile(GridUid, new Vector2i(0,0),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(GridUid, new Vector2i(-1,-1),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(GridUid, new Vector2i(0,-1),new Robust.Shared.Map.Tile(1));
        _mapSystem.SetTile(GridUid, new Vector2i(-1,0),new Robust.Shared.Map.Tile(1));
        Spawn("whore", new MapCoordinates(1, 1, MapId));
        Spawn("whore", new MapCoordinates(0, 1, MapId));
        Spawn("monu2", new MapCoordinates(1, 0, MapId));
        _mapSystem.SetAmbientLight(MapId, Color.White);
    }

    public void AddSession(ICommonSession session)
    {
        var cam = Spawn("whore", new MapCoordinates(0, 0, MapId));
        _transformSystem.AttachToGridOrMap(GridUid);
        _playerManager.SetAttachedEntity(session, cam);
    }
}