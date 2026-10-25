using Content.Shared.Physics.Components;
using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Items;

[RegisterComponent]
public sealed partial class CollectibleComponent : Component
{
    [DataField] public Vector3 Position = Vector3.Zero;
    [DataField] public EulerAngles Rotation = EulerAngles.Zero;
    [DataField] public Vector3 Scale = Vector3.One;
    [DataField] public TimeSpan CollideDelay;
    [DataField] public bool TakeAsItem = true;

    [ViewVariables] public EntityUid? TakenBy;
    [ViewVariables(VVAccess.ReadOnly)] public bool IsTaken => TakenBy != null;

    [ViewVariables] public PhysicsProperty? TakenProperties;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemCollectorComponent : Component
{
    [DataField] public string BoneName;
    [DataField, AutoNetworkedField] public EntityUid? CurrentItem;
    [ViewVariables(VVAccess.ReadOnly)] public bool IsEmpty => CurrentItem == null;
}

