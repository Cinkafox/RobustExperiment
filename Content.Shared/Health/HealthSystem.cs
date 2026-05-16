namespace Content.Shared.Health;

public sealed class HealthSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<HealthComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<HealthComponent> ent, ref ComponentInit args)
    {
        if(ent.Comp.CurrentHealth == 0) 
            ent.Comp.CurrentHealth = ent.Comp.MaxHealth;
    }

    public void ChangeHealth(Entity<HealthComponent?> ent, float value)
    {
        if(!Resolve(ent, ref ent.Comp)) return;

        var wasDead = ent.Comp.IsDead;
        var oldHealth = ent.Comp.CurrentHealth;
        ent.Comp.CurrentHealth += value;
        
        var changeEv = new OnEntityHealthChangeEvent(
            oldHealth, 
            value,
            ent.Comp.IsDead ? HealthStatus.Dead : HealthStatus.Alive);
        RaiseLocalEvent(ent, changeEv);
        
        if (!wasDead && ent.Comp.IsDead)
        {
            var ev = new OnEntityHealthStatusEvent(HealthStatus.Dead);
            RaiseLocalEvent(ent, ev);
        }
        
        if (wasDead && !ent.Comp.IsDead)
        {
            var ev = new OnEntityHealthStatusEvent(HealthStatus.Alive);
            RaiseLocalEvent(ent, ev);
        }
    }
}

[Serializable]
public sealed class OnEntityHealthChangeEvent : EntityEventArgs
{
    public float Before;
    public float After;
    public HealthStatus Status;

    public OnEntityHealthChangeEvent(float before, float after, HealthStatus status)
    {
        Before = before;
        After = after;
        Status = status;
    }
}

[Serializable]
public sealed class OnEntityHealthStatusEvent : EntityEventArgs
{
    public HealthStatus Status;

    public OnEntityHealthStatusEvent(HealthStatus status)
    {
        Status = status;
    }
}

public enum HealthStatus: byte
{
    Alive,
    Dead
}