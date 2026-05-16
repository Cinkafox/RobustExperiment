using Robust.Shared.Prototypes;

namespace Content.Shared.Weapon;

[RegisterComponent]
public sealed partial class WeaponComponent : Component
{
    [DataField(required:true)] public EntProtoId DamageAreaId;
    [DataField] public Vector3 Translation = Vector3.Zero;
    [DataField] public Vector3 Impulse = Vector3.Zero;
}