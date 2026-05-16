using Content.Shared.Physics.Data;
using Content.Shared.Weapon;

namespace Content.Shared.CollideUtil;

public sealed class DeleteOnCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DeleteOnCollideComponent, CollideObjectEvent>(OnCollide);
    }

    private void OnCollide(Entity<DeleteOnCollideComponent> ent, ref CollideObjectEvent args)
    {
        if(args.Cancelled) 
            return;
        
        QueueDel(ent);
    }
}