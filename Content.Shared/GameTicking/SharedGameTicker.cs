using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract class SharedGameTicker : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    
    public EntityUid MapUid;
    
    public void InitializeGame()
    {
        MapUid = _mapSystem.CreateMap(false);
        
        //Spawn("alexandra", new Vector3(0,-1,0), EulerAngles.CreateFromDegrees(0,0,0));
        Spawn("world", new Vector3(0,-47,0), EulerAngles.CreateFromDegrees(0,0,0));
    }

    public void AttachSession(ICommonSession session)
    {
        var camera = Spawn("camera", new Vector3(0,1,5), EulerAngles.CreateFromDegrees(0,180.0,0));
        PlayerManager.SetAttachedEntity(session, camera);
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