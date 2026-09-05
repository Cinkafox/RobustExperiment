using System.Linq;
using Content.Shared.Items;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;

namespace Content.Shared.Bone;

public sealed class BonePhysicsSystem : EntitySystem
{
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3DSystem = default!;
    [Dependency] private readonly ConstraintSystem _constraintSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BonePhysicsComponent, OnEntityAttachingEvent>(OnAttaching);
        SubscribeLocalEvent<BonePhysicsComponent, OnEntityAttachedEvent>(OnAttach);
        
        SubscribeLocalEvent<BonePhysicsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<BonePhysicsComponent> ent, ref ComponentInit args)
    {
        if(!TryComp<SkeletonComponent>(ent, out var bone))
        {
            RemComp(ent, ent.Comp);
            Log.Error($"Can't find {nameof(SkeletonComponent)} for {Name(ent.Owner)}");
            return;
        }
        
        foreach (var (key, value) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
        }
    }

    private void OnAttaching(Entity<BonePhysicsComponent> ent, ref OnEntityAttachingEvent args)
    {
        foreach (var (key, _) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
            
            RemComp<ConstraintComponent>(boneUid);
            RemComp<RigidBodyComponent>(boneUid);
            RemComp<CollectibleComponent>(boneUid);
            _transform3DSystem.SetParent(boneUid, ent);
        }
    }

    private void OnAttach(Entity<BonePhysicsComponent> ent, ref OnEntityAttachedEvent args)
    {
        foreach (var (key, bonePhysicsProperty) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var bone))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
            
            _transform3DSystem.SetParent(bone, args.To);
            var rb = AddComp<RigidBodyComponent>(bone);
            rb.Properties = bonePhysicsProperty.Property;

            foreach (var (boneName, constraint) in bonePhysicsProperty.Constraints)
            {
                if (!_boneSystem.TryGetBone(ent.Owner, boneName, out var boneB))
                {
                    Log.Error($"Can't find {boneName} from {Name(ent.Owner)}");
                    continue;
                }
                
                _constraintSystem.AddConstraint(bone, boneB, constraint);
            }

            if (bonePhysicsProperty.Takeble)
            {
                AddComp<CollectibleComponent>(bone).TakeAsItem = false;
            }
        }
    }
}