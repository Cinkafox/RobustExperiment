using Content.Shared.Utils;

namespace Content.Shared.Items;

[RegisterComponent]
public sealed partial class CollectibleComponent: Component
{
    [DataField] public Vector3 Position = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public Vector3 Scale = Vector3.One;
    
    [DataField] public EntityUid? TakenBy;
    [DataField] public TimeSpan CollideDelay;
    [ViewVariables(VVAccess.ReadOnly)] public bool IsTaken => TakenBy != null;
}

[RegisterComponent]
public sealed partial class ItemCollectorComponent: Component
{
    [DataField] public string BoneName;
    [DataField] public EntityUid? CurrentItem;
    [ViewVariables(VVAccess.ReadOnly)] public bool IsEmpty => CurrentItem == null;
}

