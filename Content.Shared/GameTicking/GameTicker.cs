using Content.Shared.States;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Content.Shared.GameTicking;

public abstract class SharedGameTicker : EntitySystem
{
    [Dependency] protected readonly IMapManager _mapManager = default!;
    [Dependency] protected readonly SharedMapSystem _mapSystem = default!;
    [Dependency] protected readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] protected readonly SharedPointLightSystem _lightSystem = default!;
    [Dependency] protected readonly IReflectionManager _reflectionManager = default!;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    public MapId MapId;
    
    public Vector2i? playerPos = null;
    public void InitializeMap()
    {
        MapUid = _mapSystem.CreateMap(out MapId);
        GridUid = _mapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        
        var width = 55;
        var height = 55;
        var generator = new MazeGenerator(width, height);
        var generated = generator.Generate();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var state = generated[x, y];

                var sx = x - width / 2;
                var sy= y - height / 2;
                
                _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
                
                if(state == 1)
                    Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
                if (state == 0 && playerPos is null) playerPos = new Vector2i(sx, sy);
            }
        }
        
        for (int y = -1; y < height+1; y++)
        {
            
            var sx = -1 - width / 2;
            var sy= y - height / 2;
                
            _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int y = -1; y < height+1; y++)
        {
            
            var sx = width - width / 2;
            var sy= y - height / 2;
                
            _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int x = -1; x < width+1; x++)
        {
            var sx = x - width / 2;
            var sy= -1 - height / 2;
                
            _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int x = -1; x < width+1; x++)
        {
            var sx = x - width / 2;
            var sy= height - height / 2;
                
            _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        _mapSystem.SetAmbientLight(MapId, Color.FromHex("#030201"));
    }

    public void AddSession(ICommonSession session)
    {
        if (playerPos is null)
        {
            Logger.Error("FUCK!");
            return;
        }
        
        var cam = Spawn("whore", new MapCoordinates(playerPos.Value.X + 0.5f, playerPos.Value.Y + 0.5f, MapId));
        _transformSystem.AttachToGridOrMap(GridUid);
        _playerManager.SetAttachedEntity(session, cam);
        _lightSystem.EnsureLight(cam);
        _lightSystem.SetColor(cam,Color.Beige);
        _lightSystem.SetRadius(cam,2f);
        
        ChangeSessionState<IGameState>(session);
    }

    public virtual void ChangeSessionState<T>(ICommonSession session) { }
}