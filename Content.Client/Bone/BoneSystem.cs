using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Bone;
using Content.Shared.Transform;

namespace Content.Client.Bone;

public sealed class BoneSystem : EntitySystem
{
    [Dependency] private readonly Transform3dSystem _transform = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BoneCompoundComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BoneComponent, ComponentInit>(OnBoneInit);
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
        
        skeletonComponent.Root = CreateBone(compound, ent.Owner, ent.Comp.Offset);
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

    private EntityUid CreateBone(BoneCompound boneCompound, EntityUid parent, Vector3 offset)
    {
        var bone = Spawn();
        _transform.SetParent(bone, parent);
        _transform.SetWorldPosition(bone, boneCompound.Position + offset);
        _transform.SetWorldRotation(bone, boneCompound.Rotation);
        var boneComp = AddComp<BoneComponent>(bone);
        
        if(boneCompound.Data is not null)
         boneComp.BoneVertexDatum = boneCompound.Data;

        if (boneCompound.Child is null) 
            return bone;
        
        foreach (var child in boneCompound.Child)
        {
            boneComp.Childs.Add(CreateBone(child, bone, offset));
        }

        return bone;
    }
    
    private void ProceedBone(Entity<BoneComponent?> entity, MeshRender mesh, Transform3dComponent? parentTransform)
    {
        if(!Resolve(entity, ref entity.Comp))
            return;

        var boneTransform = Comp<Transform3dComponent>(entity);
        

        foreach (var data in entity.Comp.BoneVertexDatum)
        {
            mesh.TranslatedVertexes[data.BoneIndices] = mesh.Mesh.Vertexes[data.BoneIndices];
            
            mesh.TranslatedVertexes[data.BoneIndices] -= entity.Comp.OriginalPosition;
            mesh.TranslatedVertexes[data.BoneIndices] =
                (boneTransform.WorldAngle - entity.Comp.OriginalRotation).RotateVec(mesh.TranslatedVertexes[data.BoneIndices]);

            mesh.TranslatedVertexes[data.BoneIndices] += entity.Comp.OriginalPosition;
            
            mesh.TranslatedVertexes[data.BoneIndices] +=
                (new Vector4(boneTransform.WorldPosition, 1f) - entity.Comp.OriginalPosition) * data.BoneWeights;
        }

        foreach (var child in entity.Comp.Childs)
        {
            ProceedBone(child, mesh, boneTransform);
        }
    }
    
    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SkeletonComponent, ModelComponent>();
        while (query.MoveNext(out var uid, out var skeleton, out var model))
        {
            if(!model.MeshRenderInitialized) return;
            
            ProceedBone(skeleton.Root, model.MeshRender, null);
        }
    }
}