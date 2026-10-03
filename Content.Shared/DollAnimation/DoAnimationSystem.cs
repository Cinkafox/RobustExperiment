using Content.Shared.Animations;
using Content.Shared.Items;
using Content.Shared.Movement;
using Content.Shared.Physics.Components;

namespace Content.Shared.DollAnimation;

public sealed class DoAnimationSystem : EntitySystem
{
    [Dependency] private BodyAnimationSystem _animationSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<DoAnimateOnMoveComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<DoAnimateOnMoveComponent, EntityStartMoveEvent>(OnStartMove);
        SubscribeLocalEvent<DoAnimateOnMoveComponent, EntityEndMoveEvent>(OnEndMove);
        SubscribeLocalEvent<DoAnimateOnMoveComponent, EntityGroundStatusChangedEvent>(OnJumping);
        
        SubscribeLocalEvent<DoAnimateOnActivateComponent, ItemUseEvent>(OnUse);
        SubscribeLocalEvent<DoAnimateOnActivateComponent, ItemDropEvent>(OnUsedDrop);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemPickupEvent>(OnPickup);
        SubscribeLocalEvent<DoAnimateWhileHoldComponent, ItemDropEvent>(OnDrop);
    }

    private void OnInit(Entity<DoAnimateOnMoveComponent> ent, ref ComponentStartup args)
    {
        //_animationSystem.Play(ent, ent.Comp.OnStandby);
    }

    private void OnJumping(Entity<DoAnimateOnMoveComponent> ent, ref EntityGroundStatusChangedEvent args)
    {
        if(!args.InGround)
        {
            _animationSystem.Play(ent, ent.Comp.OnStandby);
            if(TryComp<InputMoverComponent>(ent, out var inputMover) && inputMover.PositionMovement != Vector3.Zero)
                _animationSystem.Play(ent, ent.Comp.OnMove);
            return;
        }
        
        _animationSystem.Stop(ent, ent.Comp.OnMove);
        _animationSystem.Stop(ent, ent.Comp.OnStandby);
        _animationSystem.Play(ent, ent.Comp.OnJump);
    }


    private void OnEndMove(Entity<DoAnimateOnMoveComponent> ent, ref EntityEndMoveEvent args)
    {
        if(!IsEntityOnGround(ent))
            return;
        
        _animationSystem.Stop(ent, ent.Comp.OnMove);
        _animationSystem.Play(ent, ent.Comp.OnStandby);
    }

    private void OnStartMove(Entity<DoAnimateOnMoveComponent> ent, ref EntityStartMoveEvent args)
    {
        if(!IsEntityOnGround(ent))
            return;
        
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

    private bool IsEntityOnGround(EntityUid uid)
    {
        return TryComp<RigidBodyComponent>(uid, out var rigidBodyComponent) && rigidBodyComponent.IsGrounded;
    }
}