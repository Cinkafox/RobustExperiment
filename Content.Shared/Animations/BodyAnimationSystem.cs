using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Animations;

public abstract partial class BodyAnimationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    
    private readonly List<ProtoId<BodyAnimationPrototype>> _deleteQuery = [];
    
    public virtual void Play(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        if (!_prototypeManager.TryIndex(animationId, out var animation))
        {
            Log.Error($"Animation {animationId} not found");
            return;
        }
        
        var bodyAnimationComp = EnsureComp<BodyAnimationComponent>(uid);

        if (animation.Animation.Looped)
        {
            if(bodyAnimationComp.ActiveLoopedAnimation.Contains(animationId))
                Stop(uid, animationId);
            
            bodyAnimationComp.ActiveLoopedAnimation.Add(animationId);
        }
        else
        {
            if (bodyAnimationComp.ActiveAnimations.ContainsKey(animationId))
                Stop(uid, animationId);
            
            bodyAnimationComp.ActiveAnimations.Add(animationId, _gameTiming.CurTime + animation.Animation.Length);
        }
        
        RaiseLocalEvent(uid, new AnimationStartEvent()
        {
            AnimationId = animationId,
        });
    }

    public virtual void Stop(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        if(!TryComp<BodyAnimationComponent>(uid, out var bodyAnimationComp))
            return;

        bodyAnimationComp.ActiveAnimations.Remove(animationId);
        bodyAnimationComp.ActiveLoopedAnimation.Remove(animationId);
        
        RaiseLocalEvent(uid, new AnimationStopEvent()
        {
            AnimationId = animationId,
        });
    }
   
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityQueryEnumerator<BodyAnimationComponent>();
        while (query.MoveNext(out var uid, out var animation))
        {
            foreach (var (proto, endTime) in animation.ActiveAnimations)
            {
                if(_gameTiming.CurTime >= endTime)
                    _deleteQuery.Add(proto);
            }

            foreach (var protoId in _deleteQuery)
            {
                Stop(uid, protoId);
            }
            
            _deleteQuery.Clear();
        }
    }
}

public sealed class AnimationStartEvent : EntityEventArgs
{
    public ProtoId<BodyAnimationPrototype> AnimationId;
}

public sealed class AnimationStopEvent : EntityEventArgs
{
    public ProtoId<BodyAnimationPrototype> AnimationId;
}