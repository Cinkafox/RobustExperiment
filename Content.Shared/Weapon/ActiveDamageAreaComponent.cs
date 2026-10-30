using Robust.Shared.GameStates;

namespace Content.Shared.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveDamageAreaComponent : Component
{
    [DataField, AutoNetworkedField] public float Damage;
    [ViewVariables, AutoNetworkedField] public EntityUid DamageSource;
}