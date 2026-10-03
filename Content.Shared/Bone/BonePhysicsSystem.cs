using Content.Shared.Items;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Robust.Shared.Physics;

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
        SubscribeLocalEvent<BonePhysicsComponent, ComponentRemove>(OnRemove);
    }

    public void AddRagdoll(Entity<SkeletonComponent?> ent, Dictionary<string, BonePhysicsProperty> bonePhysics)
    {
        if(!Resolve(ent, ref ent.Comp))
            return;
        
        var comp = AddComp<BonePhysicsComponent>(ent);
        comp.BonePhysics = bonePhysics;
        var skeletEnt = new Entity<BonePhysicsComponent>(ent, comp);
        InitInternal(skeletEnt);
        MakeRagdoll(skeletEnt, Comp<Transform3dComponent>(ent).ParentUid);
        RemComp<RigidBodyComponent>(skeletEnt);
    }

    private void OnRemove(Entity<BonePhysicsComponent> ent, ref ComponentRemove args)
    {
        if(!TryComp<SkeletonComponent>(ent, out var bone))
            return;
        
        foreach (var (key, value) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }

            CleanupSkeleton(ent, boneUid, value);
            
            if(!ent.Comp.BonesParent.TryGetValue(boneUid, out var boneParentUid))
                continue;
            
            _transform3DSystem.SetParent(boneUid, boneParentUid);
        }
    }

    private void OnInit(Entity<BonePhysicsComponent> ent, ref ComponentInit args)
    {
        if(!TryComp<SkeletonComponent>(ent, out var bone))
        {
            RemComp(ent, ent.Comp);
            Log.Error($"Can't find {nameof(SkeletonComponent)} for {Name(ent.Owner)}");
            return;
        }
        
        if(ent.Comp.BonePhysics.Count != 0)
            InitInternal(ent);
    }

    private void InitInternal(Entity<BonePhysicsComponent> ent)
    {
        foreach (var (key, _) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
            
            var transform = Comp<Transform3dComponent>(boneUid);
            var parentUid = transform.ParentUid;
            
            ent.Comp.BonesParent[boneUid] = parentUid;
        }
    }

    private void OnAttaching(Entity<BonePhysicsComponent> ent, ref OnEntityAttachingEvent args)
    {
        CleanupRagdoll(ent);
    }

    private void CleanupRagdoll(Entity<BonePhysicsComponent> ent)
    {
        foreach (var (key, value) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var boneUid))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }

            CleanupSkeleton(ent, boneUid, value);
            
            _transform3DSystem.SetParent(boneUid, ent);
        }
    }

    private void CleanupSkeleton(Entity<BonePhysicsComponent> ent, EntityUid boneUid, BonePhysicsProperty physicsProperty)
    {
        foreach (var constraint in physicsProperty.Constraints)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, constraint.Key, out var boneChildUid))
            {
                continue;
            }
                
            _constraintSystem.RemoveConstraint(boneUid, boneChildUid);
        }
            
        RemComp<RigidBodyComponent>(boneUid);
        RemComp<CollectibleComponent>(boneUid);
    }

    private void OnAttach(Entity<BonePhysicsComponent> ent, ref OnEntityAttachedEvent args)
    {
        MakeRagdoll(ent, args.To);
    }

    private void MakeRagdoll(Entity<BonePhysicsComponent> ent, EntityUid attachTo)
    {
        foreach (var (key, bonePhysicsProperty) in ent.Comp.BonePhysics)
        {
            if (!_boneSystem.TryGetBone(ent.Owner, key, out var bone))
            {
                Log.Error($"Can't find {key} from {Name(ent.Owner)}");
                continue;
            }
            
            _transform3DSystem.SetParent(bone, attachTo);
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