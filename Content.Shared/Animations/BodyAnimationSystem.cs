using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Animations;

public sealed partial class BodyAnimationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    
    private readonly List<ProtoId<BodyAnimationPrototype>> _deleteQuery = [];
    
    public void Play(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
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
        Dirty(uid, bodyAnimationComp);
    }

    public void Stop(EntityUid uid, ProtoId<BodyAnimationPrototype> animationId)
    {
        if(!TryComp<BodyAnimationComponent>(uid, out var bodyAnimationComp))
            return;

        bodyAnimationComp.ActiveAnimations.Remove(animationId);
        bodyAnimationComp.ActiveLoopedAnimation.Remove(animationId);
        
        Dirty(uid, bodyAnimationComp);
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
