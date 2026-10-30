using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponComponent : Component
{
    [DataField(required:true), AutoNetworkedField] public EntProtoId DamageAreaId;
    [DataField, AutoNetworkedField] public Vector3 Translation = Vector3.Zero;
    [DataField, AutoNetworkedField] public Vector3 Impulse = Vector3.Zero;
}