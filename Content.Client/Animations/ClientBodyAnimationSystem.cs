using System.Linq;
using Content.Shared.Animations;
using Content.Shared.Bone;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using BodyAnimation = Content.Shared.Animations.Data.BodyAnimation;

namespace Content.Client.Animations;

public sealed class ClientBodyAnimationSystem : BodyAnimationSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveBodyAnimationComponent, AnimationCompletedEvent>(OnAnimationComplete);
        SubscribeLocalEvent<ActiveLoopedBodyAnimationComponent, AnimationCompletedEvent>(OnAnimationLoopedComplete);
    }

    private void OnAnimationLoopedComplete(Entity<ActiveLoopedBodyAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if(!args.Finished)
            return;
        
        _animationPlayer.Play(ent, ent.Comp.CurrentAnimation, args.Key);
    }

    private void OnAnimationComplete(Entity<ActiveBodyAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if(!args.Finished || 
           !TryComp<BodyAnimationComponent>(ent.Comp.MainUid, out var bodyAnimation) || 
           !bodyAnimation.ActiveAnimations.TryGetValue(args.Key, out var animationSpan) ||
           (_gameTiming.CurTime >= animationSpan)) 
            return;
        
        _animationPlayer.Play(ent, ent.Comp.CurrentAnimation, args.Key);
    }

    public override void Play(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        base.Play(uid, animationId);
        
        if(!_prototypeManager.TryIndex(animationId, out var proto))
            return;
        
        var animations = GetAnimation(uid, proto.Animation);
        
        foreach (var animation in animations)
        {
            if (animation.Looped)
            {
                ProceedAnimationPlay<ActiveLoopedBodyAnimationComponent>(uid, animation, animationId);
            }
            else
            {
                ProceedAnimationPlay<ActiveBodyAnimationComponent>(uid, animation, animationId);
            }
        }
    }

    private void ProceedAnimationPlay<T>(EntityUid uid, 
        CurrentAnimationProperty property, 
        ProtoId<BodyAnimationPrototype> animationId) 
        where T: Component, IActiveBodyAnimation, new()
    {
        if(TryComp<T>(property.EntityUid, out var activeComp))
        {
            _animationPlayer.Stop(property.EntityUid, animationId);
            RemComp<T>(property.EntityUid);
        }
            
        activeComp = AddComp<T>(property.EntityUid);
        activeComp.MainUid = uid;
        activeComp.CurrentAnimation = property.Animation;
        activeComp.AnimationId = animationId;
        
        _animationPlayer.Play(property.EntityUid, property.Animation, animationId);
    }

    public override void Stop(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        base.Stop(uid, animationId);
        if(!_prototypeManager.TryIndex(animationId, out var proto))
            return;
        
        var animations = GetAnimation(uid, proto.Animation);

        foreach (var animation in animations)
        {
            _animationPlayer.Stop(animation.EntityUid, animationId);
            if (animation.Looped)
                RemComp<ActiveLoopedBodyAnimationComponent>(animation.EntityUid);
            else
            {
                RemComp<ActiveBodyAnimationComponent>(animation.EntityUid);
                //ResumeLoopedAnimation(animation.EntityUid);
            }
        }
    }

    private void ResumeLoopedAnimation(EntityUid playingUid)
    {
        if(!TryComp<ActiveLoopedBodyAnimationComponent>(playingUid, out var activeComp))
            return;
        
        _animationPlayer.Play(playingUid, activeComp.CurrentAnimation, activeComp.AnimationId);
    }

    private List<CurrentAnimationProperty> GetAnimation(EntityUid uid, BodyAnimation animation)
    {
        var animaList = new Dictionary<EntityUid, Animation>();
        
        foreach (var rawTrack in animation.Tracks)
        {
            var track = new AnimationTrackComponentProperty
            {
                InterpolationMode = rawTrack.InterpolationMode,
                ComponentType = rawTrack.ComponentType,
                Property = rawTrack.Property,
                KeyFrames = rawTrack.KeyFrames.KeyFrames.Select(b => new AnimationTrackProperty.KeyFrame(b.Value, b.KeyTime)).ToList()
            };

            if (rawTrack.Bone is not null)
            {
                if (!_boneSystem.TryGetBone(uid, rawTrack.Bone, out var bone))
                {
                    Log.Error($"Could not find bone {rawTrack.Bone}");
                    continue;
                }
                
                if(!animaList.TryGetValue(bone, out var boneAnimation))
                {
                    boneAnimation = new Animation();
                    boneAnimation.Length = animation.Length;
                    animaList.Add(bone, boneAnimation);
                }
            
                boneAnimation.AnimationTracks.Add(track);
                
                continue;
            }
            
            if(!animaList.TryGetValue(uid, out var mainAnimation))
            {
                mainAnimation = new Animation();
                mainAnimation.Length = animation.Length;
                animaList.Add(uid, mainAnimation);
            }
            
            mainAnimation.AnimationTracks.Add(track);
        }
        
        return animaList.Select(kv => new CurrentAnimationProperty(kv.Key, kv.Value, animation.Looped)).ToList();
    }
    
}

public record struct CurrentAnimationProperty(EntityUid EntityUid, Animation Animation, bool Looped);
