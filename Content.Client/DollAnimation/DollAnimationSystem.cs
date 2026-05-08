using Content.Shared.Bone;
using Robust.Client.GameObjects;

namespace Content.Client.DollAnimation;

public sealed class DollAnimationSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BoneOnLoopAnimationComponent, AnimationCompletedEvent>(OnAnimationEnd);
    }
    
    private void OnAnimationEnd(Entity<BoneOnLoopAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        var ownerUid = ent.Comp.BoneOwner;
        
        if(!TryComp<DollAnimationComponent>(ownerUid, out var dollAnimation)) 
            return;
        
        if(!dollAnimation.RegisteredAnimations.TryGetValue(args.Key, out var property) || !property.Looped)
            return;
        
        if(!dollAnimation.CurrentBoneLoopedAnimations.TryGetValue(property.BoneName, out var otherAnimation))
            return;
        
        if(!args.Key.Equals(otherAnimation))
            return;
        
        _animationPlayer.Play(ent.Owner, property.Value, args.Key);
    }

    public void RegisterAnimation(Entity<DollAnimationComponent?> entity, string animationName, AnimationProperty property)
    {
        entity.Comp = EnsureComp<DollAnimationComponent>(entity);
        entity.Comp.RegisteredAnimations[animationName] = property;
    }
    
    public void PlayAnimation(Entity<DollAnimationComponent?> entity, string animationName)
    {
        if(!Resolve(entity, ref entity.Comp) || 
           !entity.Comp.RegisteredAnimations.TryGetValue(animationName, out var property) ||
           !_boneSystem.TryGetBone(entity.Owner, property.BoneName, out var boneUid))
            return;

        if (property.Looped)
        {
            if (entity.Comp.CurrentBoneLoopedAnimations.TryGetValue(property.BoneName, out var otherAnimation))
            {
                RemComp<BoneOnLoopAnimationComponent>(boneUid);
                entity.Comp.CurrentBoneLoopedAnimations.Remove(property.BoneName);
                _animationPlayer.Stop(boneUid, otherAnimation);
            }
            
            AddComp<BoneOnLoopAnimationComponent>(boneUid).BoneOwner = entity;
            entity.Comp.CurrentBoneLoopedAnimations.Add(property.BoneName, animationName);
        }
        else
        {
            if (entity.Comp.CurrentBoneAnimations.TryGetValue(property.BoneName, out var otherAnimation))
            {
                RemComp<BoneOnAnimationComponent>(boneUid);
                entity.Comp.CurrentBoneAnimations.Remove(property.BoneName);
                _animationPlayer.Stop(boneUid, otherAnimation);
            }
            
            AddComp<BoneOnAnimationComponent>(boneUid).BoneOwner = entity;
            entity.Comp.CurrentBoneAnimations.Add(property.BoneName, animationName);
        }

        _animationPlayer.Play(boneUid, property.Value, animationName);
    }

    public void StopAnimation(Entity<DollAnimationComponent?> entity, string animationName)
    {
        if(!Resolve(entity, ref entity.Comp) || 
           !entity.Comp.RegisteredAnimations.TryGetValue(animationName, out var property) ||
           !_boneSystem.TryGetBone(entity.Owner, property.BoneName, out var boneUid))
            return;
        
        entity.Comp.CurrentBoneAnimations.Remove(property.BoneName);
        _animationPlayer.Stop(boneUid, animationName);
    }
}