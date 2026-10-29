using System.Linq;
using Content.Shared.Animations;
using Content.Shared.Bone;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using BodyAnimation = Content.Shared.Animations.Data.BodyAnimation;

namespace Content.Client.Animations;

public sealed partial class ClientBodyAnimationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private BoneSystem _boneSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ActiveBodyAnimationComponent, AnimationCompletedEvent>(OnAnimationComplete);
        SubscribeLocalEvent<ActiveLoopedBodyAnimationComponent, AnimationCompletedEvent>(OnAnimationLoopedComplete);
        SubscribeLocalEvent<BodyAnimationComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, BodyAnimationComponent component, ComponentInit args)
    {
        EnsureComp<ClientBodyAnimComponent>(uid);
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
        
        if(!_animationPlayer.HasRunningAnimation(ent, args.Key))
            _animationPlayer.Play(ent, ent.Comp.CurrentAnimation, args.Key);
    }

    public void PlayClient(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
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
    
    public void StopClient(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        if(!_prototypeManager.TryIndex(animationId, out var proto))
            return;
        
        var animations = GetAnimation(uid, proto.Animation);

        foreach (var animation in animations)
        {
            _animationPlayer.Stop(animation.EntityUid, animationId);

            if (animation.Looped)
            {
                if (TryComp<ActiveLoopedBodyAnimationComponent>(animation.EntityUid, out var active) &&
                    active.AnimationId == animationId)
                    RemComp<ActiveLoopedBodyAnimationComponent>(animation.EntityUid);
            }
            else
            {
                if (TryComp<ActiveBodyAnimationComponent>(animation.EntityUid, out var active) &&
                    active.AnimationId == animationId)
                    RemComp<ActiveBodyAnimationComponent>(animation.EntityUid);
            }
        }
    }

    private void ProceedAnimationPlay<T>(EntityUid uid, 
        CurrentAnimationProperty property, 
        ProtoId<BodyAnimationPrototype> animationId) 
        where T: Component, IActiveBodyAnimation, new()
    {
        StopActiveAnimation(property.EntityUid);

        var activeComp = AddComp<T>(property.EntityUid);
        activeComp.MainUid = uid;
        activeComp.CurrentAnimation = property.Animation;
        activeComp.AnimationId = animationId;
        
        if(!_animationPlayer.HasRunningAnimation(property.EntityUid, animationId))
            _animationPlayer.Play(property.EntityUid, property.Animation, animationId);
    }

    private void StopActiveAnimation(EntityUid entityUid)
    {
        if (TryComp<ActiveLoopedBodyAnimationComponent>(entityUid, out var looped))
        {
            _animationPlayer.Stop(entityUid, looped.AnimationId);
            RemComp<ActiveLoopedBodyAnimationComponent>(entityUid);
        }

        if (TryComp<ActiveBodyAnimationComponent>(entityUid, out var active))
        {
            _animationPlayer.Stop(entityUid, active.AnimationId);
            RemComp<ActiveBodyAnimationComponent>(entityUid);
        }
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

    //TODO: come up with a more elegant solution to this problem
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BodyAnimationComponent, ClientBodyAnimComponent>();
        while (query.MoveNext(out var uid, out var component, out var clientBodyAnimationComp))
        {
            foreach (var (protoId, value) in component.ActiveAnimations)
            {
                if(clientBodyAnimationComp.ActiveAnimations.ContainsKey(protoId)) 
                    continue;
            
                PlayClient(uid, protoId);
                clientBodyAnimationComp.ActiveAnimations.Add(protoId, value);
            }

            foreach (var (protoId, value) in clientBodyAnimationComp.ActiveAnimations.ToDictionary(kv => kv.Key, kv => kv.Value))
            {
                if(component.ActiveAnimations.ContainsKey(protoId)) 
                    continue;
            
                StopClient(uid, protoId);
                clientBodyAnimationComp.ActiveAnimations.Remove(protoId);
            }
        
            foreach (var protoId in component.ActiveLoopedAnimation)
            {
                if(clientBodyAnimationComp.ActiveLoopedAnimation.Contains(protoId)) 
                    continue;
            
                PlayClient(uid, protoId);
                clientBodyAnimationComp.ActiveLoopedAnimation.Add(protoId);
            }

            foreach (var protoId in clientBodyAnimationComp.ActiveLoopedAnimation.ToHashSet())
            {
                if(component.ActiveLoopedAnimation.Contains(protoId)) 
                    continue;
            
                StopClient(uid, protoId);
                clientBodyAnimationComp.ActiveLoopedAnimation.Remove(protoId);
            }
        }
    }
}

public record struct CurrentAnimationProperty(EntityUid EntityUid, Animation Animation, bool Looped);
