using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Bone;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Client.Bone;

public sealed class BoneSystem : EntitySystem
{
    [Dependency] private readonly Transform3dSystem _transform = default!;
    
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
        
        skeletonComponent.Root = CreateBone(compound, ent.Owner, ent.Comp.Offset, skeletonComponent);
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

    public bool TryGetBone(Entity<SkeletonComponent?> entity, string boneName, out EntityUid bone)
    {
        bone = default;
        return Resolve(entity, ref entity.Comp) && entity.Comp.BonesDictionary.TryGetValue(boneName, out bone);
    }

    private EntityUid CreateBone(BoneCompound boneCompound, EntityUid parent, Vector3 offset, SkeletonComponent rootComponent)
    {
        var bone = Spawn();
        _transform.SetParent(bone, parent);
        _transform.SetWorldPosition(bone, boneCompound.Position + offset);
        _transform.SetWorldRotation(bone, boneCompound.Rotation);
        var boneComp = AddComp<BoneComponent>(bone);
        
        if(boneCompound.Data is not null)
            boneComp.BoneVertexDatum = boneCompound.Data;

        if (!string.IsNullOrEmpty(boneCompound.Name))
            rootComponent.BonesDictionary[boneCompound.Name] = bone;

        if (boneCompound.Child is null) 
            return bone;
        
        foreach (var child in boneCompound.Child)
        {
            boneComp.Children.Add(CreateBone(child, bone, offset, rootComponent));
        }

        return bone;
    }
    
    private void ProceedBone(Entity<BoneComponent?> entity, MeshRender mesh, Transform3dComponent parentTransform)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var boneTransform = Comp<Transform3dComponent>(entity);
        
        var originalQ = entity.Comp.OriginalRotation.ToQuaternion();
        var invOriginalQ = Quaternion.Inverse(originalQ);
        var currentQ = boneTransform.WorldRotation;
        var invCurrentQ = Quaternion.Inverse(currentQ);
        var deltaRotation = Quaternion.Normalize(currentQ * invOriginalQ);

        var originalPos = new Vector3(
            entity.Comp.OriginalPosition.X,
            entity.Comp.OriginalPosition.Y,
            entity.Comp.OriginalPosition.Z
        );

        var currentPos = boneTransform.WorldPosition;
        var deltaPos = currentPos - originalPos;

        foreach (var data in entity.Comp.BoneVertexDatum)
        {
            var index = data.BoneIndices;
            var v = mesh.Mesh.Vertexes[index];
            var pos = new Vector3(v.X, v.Y, v.Z);
            
            pos -= originalPos;
            pos = Matrix4Helpers.TransformVector(pos, deltaRotation);
            pos += originalPos;
            
            pos += deltaPos * data.BoneWeights;
            mesh.TranslatedVertexes[index] = new Vector4(pos, 1f);
        }
        
        foreach (var child in entity.Comp.Children)
        {
            ProceedBone(child, mesh, boneTransform);
        }
    }
    
    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SkeletonComponent, ModelComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var skeleton, out var model, out var transform))
        {
            if(!model.MeshRenderInitialized) continue;
            
            ProceedBone(skeleton.Root, model.MeshRender, transform);
        }
    }
}