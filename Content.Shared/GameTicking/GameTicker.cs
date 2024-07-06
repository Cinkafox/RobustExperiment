using Content.Shared.State;
using Content.Shared.States;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Content.Shared.GameTicking;

public abstract class SharedGameTicker : EntitySystem
{
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedMapSystem MapSystem = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedPointLightSystem LightSystem = default!;
    [Dependency] protected readonly IContentStateManager ContentStateManager = default!;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    public MapId MapId;
    
    public Vector2i? playerPos = null;
    public void InitializeMap()
    {
        MapUid = MapSystem.CreateMap(out MapId);
        GridUid = MapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        
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
                
                MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
                
                //if(state == 1)
                //    Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
                if (state == 0 && playerPos is null) playerPos = new Vector2i(sx, sy);
            }
        }
        
        for (int y = -1; y < height+1; y++)
        {
            
            var sx = -1 - width / 2;
            var sy= y - height / 2;
                
            MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int y = -1; y < height+1; y++)
        {
            
            var sx = width - width / 2;
            var sy= y - height / 2;
                
            MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int x = -1; x < width+1; x++)
        {
            var sx = x - width / 2;
            var sy= -1 - height / 2;
                
            MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        for (int x = -1; x < width+1; x++)
        {
            var sx = x - width / 2;
            var sy= height - height / 2;
                
            MapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
            Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
        }
        
        MapSystem.SetAmbientLight(MapId, Color.FromHex("#ffffff"));
    }

    public void AddSession(ICommonSession session)
    {
        if (playerPos is null)
        {
            Logger.Error("FUCK!");
            return;
        }
        
        var cam = Spawn("car", new MapCoordinates(playerPos.Value.X + 0.5f, playerPos.Value.Y + 0.5f, MapId));
        TransformSystem.AttachToGridOrMap(GridUid);
        PlayerManager.SetAttachedEntity(session, cam);
        
        ContentStateManager.ChangeSessionState<IGameState>(session);
    }
}