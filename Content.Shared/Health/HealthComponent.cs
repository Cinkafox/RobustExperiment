namespace Content.Shared.Health;

[RegisterComponent]
public sealed partial class HealthComponent: Component
{
    [DataField] public float MaxHealth = 100;
    [DataField] public float CurrentHealth
    {
        get => field;
        set => field = Math.Clamp(value, 0, MaxHealth);
    }
    [ViewVariables(VVAccess.ReadOnly)] public bool IsDead => CurrentHealth <= 0;
}