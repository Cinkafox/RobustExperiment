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
    }

    private void OnAnimationComplete(Entity<ActiveBodyAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if(!args.Finished || 
           !TryComp<BodyAnimationComponent>(ent.Comp.MainUid, out var bodyAnimation) || 
           !bodyAnimation.ActiveAnimations.TryGetValue(args.Key, out var animationSpan) ||
           (_gameTiming.CurTime >= animationSpan && !ent.Comp.IsLooped)) 
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
            _animationPlayer.Play(animation.Item1, animation.Item2, animationId);
            var activeComp = AddComp<ActiveBodyAnimationComponent>(animation.Item1);
            activeComp.MainUid = uid;
            activeComp.CurrentAnimation = animation.Item2;
            activeComp.IsLooped = animation.Item3;
        }
    }

    public override void Stop(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        base.Stop(uid, animationId);
        if(!_prototypeManager.TryIndex(animationId, out var proto))
            return;
        
        var animations = GetAnimation(uid, proto.Animation);

        foreach (var animation in animations)
        {
            _animationPlayer.Stop(animation.Item1, animationId);
            RemComp<ActiveBodyAnimationComponent>(animation.Item1);
        }
    }

    public List<(EntityUid, Animation, bool)> GetAnimation(EntityUid uid, BodyAnimation animation)
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
        
        return animaList.Select(kv => (uid, kv.Value, animation.Looped)).ToList();
    }
    
}
