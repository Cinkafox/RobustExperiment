using Content.Shared.Utils;
using Robust.Shared.Prototypes;

namespace Content.Shared.Location;

[Prototype]
public sealed partial class LocationPrototype: IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;
    [DataField] public List<LocationEntityEntry> Entities { get; set; } = [];
    [DataField] public LocationEntityEntry PlayerSpawn { get; set; } = new()
    {
        Entity = "camera"
    };
}

[DataDefinition]
public sealed partial class LocationEntityEntry
{
    [DataField] public Vector3 Position = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public EntProtoId Entity;
}