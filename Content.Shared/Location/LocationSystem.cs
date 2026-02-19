using Content.Shared.Transform;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Location;

public sealed class LocationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3dSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    
    private EntityUid? _mapUid;
    private LocationEntityEntry _playerEntitySpawn = new LocationEntityEntry()
    {
        Entity = "camera",
    };
    
    public EntityUid MapUid => _mapUid ?? throw new NullReferenceException("Location is not initialized.");

    public void LoadLocation(ProtoId<LocationPrototype> locationId)
    {
        if(!_prototypeManager.TryIndex(locationId, out var prototype))
        {
            Log.Error($"Unable to load location {locationId}");
            return;
        }

        if(_mapUid.HasValue) Del(_mapUid.Value);
        _mapUid = _mapSystem.CreateMap();

        _playerEntitySpawn = prototype.PlayerSpawn;

        foreach (var entity in prototype.Entities)
        {
            Spawn(entity);
        }
    }

    public void AttachSession(ICommonSession session)
    {
        var entity = Spawn(_playerEntitySpawn);
        _playerManager.SetAttachedEntity(session, entity);
    }

    private EntityUid Spawn(LocationEntityEntry entity)
    {
        var ent = Spawn(entity.Entity);
        _transform3dSystem.SetParent(ent, MapUid);
        _transform3dSystem.SetWorldPosition(ent, entity.Position);
        _transform3dSystem.SetWorldRotation(ent, entity.Rotation);
        return ent;
    }
}