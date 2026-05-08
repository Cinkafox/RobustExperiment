namespace Content.Shared.Weapon;

[RegisterComponent]
public sealed partial class ActiveDamageAreaComponent: Component
{
    [DataField] public float Damage;
    [ViewVariables] public EntityUid DamageSource;
}