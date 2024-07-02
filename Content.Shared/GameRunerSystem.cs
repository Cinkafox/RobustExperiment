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
    [Dependency] protected readonly SharedPointLightSystem _lightSystem = default!;
    
    public EntityUid MapUid;
    public Entity<MapGridComponent> GridUid;
    public MapId MapId;
    
    public Vector2i? playerPos = null;
    public void InitializeMap()
    {
        MapUid = _mapSystem.CreateMap(out MapId);
        GridUid = _mapManager.CreateGridEntity(MapUid,GridCreateOptions.Default);
        
        var width = 25;
        var height = 25;
        var generator = new MazeGenerator(width, height);
        var generated = generator.Generate();
        
        for (int y = 0; y < height; y++)
        {
            var dbgMess = "";
            for (int x = 0; x < width; x++)
            {
                var state = generated[x, y];
                dbgMess += state + "";

                var sx = x - width / 2;
                var sy= y - height / 2;
                
                _mapSystem.SetTile(GridUid, new Vector2i(sx,sy),new Robust.Shared.Map.Tile(1));
                
                if(state == 1)
                    Spawn("wall", new MapCoordinates(sx + 0.5f,sy +  0.5f, MapId));
                if (state == 0 && playerPos is null) playerPos = new Vector2i(sx, sy);
            }
            Logger.Debug(dbgMess);
        }
        
        _mapSystem.SetAmbientLight(MapId, Color.FromHex("#ffffff"));
    }

    public void AddSession(ICommonSession session)
    {
        if (playerPos is null)
        {
            Logger.Error("FUCK!");
            return;
        }
        
        var cam = Spawn("whore", new MapCoordinates(playerPos.Value.X, playerPos.Value.Y, MapId));
        _transformSystem.AttachToGridOrMap(GridUid);
        _playerManager.SetAttachedEntity(session, cam);
        _lightSystem.EnsureLight(cam);
        _lightSystem.SetColor(cam,Color.Beige);
    }
}

public sealed class MazeGenerator
{
    private int width, height;
    private int[,] maze;
    private Random rand = new Random();

    public MazeGenerator(int width, int height)
    {
        this.width = width;
        this.height = height;
        maze = new int[height, width];
    }

    public int[,] Generate()
    {
        // Заполнить лабиринт стенами
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                maze[y, x] = 1;
            }
        }

        // Начальная точка
        int startX = rand.Next(width / 2) * 2;
        int startY = rand.Next(height / 2) * 2;
        maze[startY, startX] = 0;

        GeneratePath(startX, startY);

        return maze;
    }

    private void GeneratePath(int x, int y)
    {
        // Случайный порядок направления движения
        int[] directions = new int[] { 0, 1, 2, 3 };
        Shuffle(directions);

        foreach (var direction in directions)
        {
            int newX = x, newY = y;

            switch (direction)
            {
                case 0: newY -= 2; break; // Вверх
                case 1: newY += 2; break; // Вниз
                case 2: newX -= 2; break; // Влево
                case 3: newX += 2; break; // Вправо
            }

            if (IsInMaze(newX, newY) && maze[newY, newX] == 1)
            {
                maze[newY, newX] = 0;
                maze[(newY + y) / 2, (newX + x) / 2] = 0;
                GeneratePath(newX, newY);
            }
        }
    }

    private bool IsInMaze(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private void Shuffle(IList<int> array)
    {
        for (var i = array.Count - 1; i > 0; i--)
        {
            var j = rand.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}