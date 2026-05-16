using Content.Shared.DollAnimation;
using Content.Shared.Items;

namespace Content.Client.DollAnimation;

public sealed class DoAnimateOnActivateSystem : EntitySystem
{
    [Dependency] private readonly DollAnimationSystem _dollAnimationSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<DoAnimateOnActivateComponent, ItemUseEvent>(OnUse);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemPickupEvent>(OnPickup);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemDropEvent>(OnDrop);
    }

    private void OnDrop(Entity<DoAnimateWhileHoldComponent> ent, ref ItemDropEvent args)
    {
        _dollAnimationSystem.StopAnimation(args.DroppedBy,  ent.Comp.AnimationName);
    }

    private void OnPickup(Entity<DoAnimateWhileHoldComponent> ent, ref ItemPickupEvent args)
    {
        _dollAnimationSystem.PlayAnimation(args.PickedBy,  ent.Comp.AnimationName);
    }

    private void OnUse(Entity<DoAnimateOnActivateComponent> ent, ref ItemUseEvent args)
    {
        _dollAnimationSystem.PlayAnimation(args.UsedBy, ent.Comp.AnimationName);
    }
}