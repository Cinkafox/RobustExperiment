using Content.Shared.Animations;
using Content.Shared.Items;
using Content.Shared.Movement;

namespace Content.Shared.DollAnimation;

public sealed class DoAnimationSystem : EntitySystem
{
    [Dependency] private BodyAnimationSystem _animationSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoAnimateOnMoveComponent, EntityStartMoveEvent>(OnStartMove);
        SubscribeLocalEvent<DoAnimateOnMoveComponent, EntityEndMoveEvent>(OnEndMove);
        
        SubscribeLocalEvent<DoAnimateOnActivateComponent, ItemUseEvent>(OnUse);
        SubscribeLocalEvent<DoAnimateOnActivateComponent, ItemDropEvent>(OnUsedDrop);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemPickupEvent>(OnPickup);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemDropEvent>(OnDrop);
    }

    private void OnEndMove(Entity<DoAnimateOnMoveComponent> ent, ref EntityEndMoveEvent args)
    {
        _animationSystem.Stop(ent, ent.Comp.OnMove);
        _animationSystem.Play(ent, ent.Comp.OnStandby);
    }

    private void OnStartMove(Entity<DoAnimateOnMoveComponent> ent, ref EntityStartMoveEvent args)
    {
        _animationSystem.Stop(ent, ent.Comp.OnStandby);
        _animationSystem.Play(ent, ent.Comp.OnMove);
    }
    
    private void OnUsedDrop(Entity<DoAnimateOnActivateComponent> ent, ref ItemDropEvent args)
    {
        _animationSystem.Stop(args.DroppedBy,  ent.Comp.AnimationName);
    }

    private void OnDrop(Entity<DoAnimateWhileHoldComponent> ent, ref ItemDropEvent args)
    {
        _animationSystem.Stop(args.DroppedBy,  ent.Comp.AnimationName);
    }

    private void OnPickup(Entity<DoAnimateWhileHoldComponent> ent, ref ItemPickupEvent args)
    {
        _animationSystem.Play(args.PickedBy,  ent.Comp.AnimationName);
    }

    private void OnUse(Entity<DoAnimateOnActivateComponent> ent, ref ItemUseEvent args)
    {
        _animationSystem.Play(args.UsedBy, ent.Comp.AnimationName);
    }
}