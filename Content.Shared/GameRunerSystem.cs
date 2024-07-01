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
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;

    public static int[] SimpleMap = new[]
    {
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1,
        1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1,
        1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    };

    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    public MapId MapId;
    public void InitializeMap()
    {
        MapUid = _mapSystem.CreateMap(out MapId);
        GridUid = _mapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);

        for (var i = 0; i < SimpleMap.Length; i++)
        {
            var x = i % 12;
            var y = i / 12;
            
            _mapSystem.SetTile(GridUid, new Vector2i(x,y),new Robust.Shared.Map.Tile(1));
            
            if(SimpleMap[i] == 1)
                Spawn("wall", new MapCoordinates(x + 0.5f,y +  0.5f, MapId));
        }
        
        _mapSystem.SetAmbientLight(MapId, Color.White);
    }

    public void AddSession(ICommonSession session)
    {
        var cam = Spawn("whore", new MapCoordinates(0, 0, MapId));
        _transformSystem.AttachToGridOrMap(GridUid);
        _playerManager.SetAttachedEntity(session, cam);
    }
}