using Robust.Shared.GameStates;

namespace Content.Shared.Health;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class HealthComponent : Component
{
    [DataField, AutoNetworkedField] public float MaxHealth = 100;
    [DataField, AutoNetworkedField] public float CurrentHealth = 100;

    [ViewVariables(VVAccess.ReadOnly)] public bool IsDead => CurrentHealth <= 0;
}