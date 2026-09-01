using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;

namespace Content.Shared.Bone;

public sealed class BonePhysicsSystem : EntitySystem
{
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3DSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BonePhysicsComponent, ComponentInit>(OnPhysicsInit);
    }

    private void OnPhysicsInit(Entity<BonePhysicsComponent> ent, ref ComponentInit args)
    {
        if(!TryComp<SkeletonComponent>(ent, out var bone))
        {
            RemComp(ent, ent.Comp);
            Log.Error($"Can't find {nameof(SkeletonComponent)} for {Name(ent.Owner)}");
            return;
        }

        var transform = EnsureComp<Transform3dComponent>(ent.Owner);
        
        foreach (var (key, value) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
            
            var boneTransform = EnsureComp<Transform3dComponent>(boneUid);

            var proxyUid = Spawn();
            var proxyComp = EnsureComp<BoneProxyComponent>(proxyUid);
            proxyComp.ProxyUid = boneUid;
            proxyComp.OwnerUid = ent.Owner;
            
            _transform3DSystem.SetParent(proxyUid, transform.ParentUid);
            
            Log.Warning($"Proxy {proxyComp.ProxyUid} => {proxyUid}");
            
            var physComp = EnsureComp<RigidBodyComponent>(proxyUid);
            physComp.Properties = value;

            ent.Comp.Proxies[boneUid] = proxyUid;
           
            if(!ent.Comp.Proxies.TryGetValue(boneTransform.ParentUid, out var parentProxy))
            {
                physComp.PhysType = PhysType.Static;
                continue;
            }
        
            Log.Warning($"constr for {proxyUid} -- {parentProxy}");
            var constrComp = EnsureComp<ConstraintComponent>(proxyUid);
            constrComp.ConstraintUid = parentProxy;
            constrComp.Constraint = new PointToPointConstraint()
            {
                LocalAnchorA = boneTransform.WorldPosition * 2,
                LocalAnchorB = EnsureComp<Transform3dComponent>(boneTransform.ParentUid).WorldPosition * 2,
            };
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        
        var query = EntityQueryEnumerator<Transform3dComponent, BoneProxyComponent>();
        while (query.MoveNext(out var transform, out var boneProxy))
        {
            if(!TryComp<Transform3dComponent>(boneProxy.ProxyUid, out var proxyTransform))
                continue;
            
            if (!transform.ParentUid.Valid)
            {
                var ownerTransform = EnsureComp<Transform3dComponent>(boneProxy.OwnerUid);
                
                if(!ownerTransform.ParentUid.Valid) 
                    continue;
                _transform3DSystem.SetParent(transform.Owner, ownerTransform.ParentUid);
                continue;
            }
            
            continue;
            
            proxyTransform.WorldPosition = transform.WorldPosition;
            proxyTransform.WorldRotation = transform.WorldRotation;
            proxyTransform.WorldScale = transform.WorldScale;
        }
    }
}