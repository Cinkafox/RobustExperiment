using System.Numerics;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Shared.Bone;

public sealed partial class BoneSystem : EntitySystem
{
    [Dependency] private Transform3dSystem _transform = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BoneCompoundComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BoneComponent, ComponentInit>(OnBoneInit);
        SubscribeLocalEvent<BoneComponent, ComponentRemove>(OnRemoved);
    }

    private void OnRemoved(Entity<BoneComponent> ent, ref ComponentRemove args)
    {
        
    }

    private void OnBoneInit(Entity<BoneComponent> ent, ref ComponentInit args)
    {
        var transform = Comp<Transform3dComponent>(ent);
        ent.Comp.OriginalPosition = new Vector4(transform.WorldPosition, 1f);
        ent.Comp.OriginalRotation = transform.WorldAngle;
    }

    private void OnComponentInit(Entity<BoneCompoundComponent> ent, ref ComponentInit args)
    {
        if(!TryComp<SkeletonComponent>(ent, out var skeletonComponent) || 
           skeletonComponent.Root.Valid) return;

        var compound = GetCompoundRecursive(ent);

        if (compound == null)
            return;
        
        var restTransform = Comp<Transform3dComponent>(ent);
        var restWorld = restTransform.WorldMatrix;
        var restRotation = restTransform.WorldRotation;

        skeletonComponent.OriginalWorldMatrix = restWorld;
        skeletonComponent.Root = CreateBone(compound, ent.Owner, ent.Comp.Offset,
            new Entity<SkeletonComponent>(ent, skeletonComponent), restWorld, restRotation);
    }

    public BoneCompound? GetCompoundRecursive(EntityUid uid)
    {
        if (!TryComp<BoneCompoundComponent>(uid, out var bone)) 
            return null;

        if (bone.Compound != null)
            return bone.Compound;
        
        if (bone.CompoundEnt.HasValue)
            return GetCompoundRecursive(bone.CompoundEnt.Value);

        if (!bone.CompoundEntId.HasValue)
            return null;

        var spawnedThink = Spawn(bone.CompoundEntId.Value);
        bone.CompoundEnt = spawnedThink;
        
        return GetCompoundRecursive(spawnedThink);
    }

    public bool TryGetBone(Entity<SkeletonComponent?> entity, string? boneName,out EntityUid bone)
    {
        bone = default;
        if (boneName == null) return false;
        return Resolve(entity, ref entity.Comp) && entity.Comp.BonesDictionary.TryGetValue(boneName, out bone);
    }

    private EntityUid CreateBone(BoneCompound boneCompound, EntityUid parent, Vector3 offset,
        Entity<SkeletonComponent> skeleton, Matrix4x4 restWorld, Quaternion restRotation)
    {
        var bone = Spawn();
        _transform.SetParent(bone, parent);
        _transform.SetWorldPosition(bone, Vector3.Transform(boneCompound.Position + offset, restWorld));
        _transform.SetWorldRotation(bone, Quaternion.Multiply(restRotation, boneCompound.Rotation.ToQuaternion()));
        var boneComp = AddComp<BoneComponent>(bone);
        boneComp.Skeleton = skeleton;
        
        if(boneCompound.Data is not null)
            boneComp.BoneVertexDatum = boneCompound.Data;

        if (!string.IsNullOrEmpty(boneCompound.Name))
            skeleton.Comp.BonesDictionary[boneCompound.Name] = bone;

        if (boneCompound.Child is null) 
            return bone;
        
        foreach (var child in boneCompound.Child)
        {
            boneComp.Children.Add(CreateBone(child, bone, offset, skeleton, restWorld, restRotation));
        }

        return bone;
    }
}