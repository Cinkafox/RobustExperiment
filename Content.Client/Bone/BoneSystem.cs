﻿using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Client.Bone;

public sealed class BoneSystem : EntitySystem
{
    [Dependency] private readonly Transform3dSystem _transform = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<SkeletonComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BoneComponent, ComponentInit>(OnBoneInit);
    }

    private void OnBoneInit(Entity<BoneComponent> ent, ref ComponentInit args)
    {
        var transform = Comp<Transform3dComponent>(ent);
        ent.Comp.OriginalPosition = transform.WorldPosition;
        ent.Comp.OriginalRotation = transform.WorldRotation;
    }

    private void OnComponentInit(Entity<SkeletonComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Root.Valid)
            return;
        
        if(ent.Comp.Compound is null)
        {
            RemComp(ent, ent.Comp);
            return;
        }

        ent.Comp.Root = CreateBone(ent.Comp.Compound, ent);
    }

    private EntityUid CreateBone(BoneCompound boneCompound, EntityUid parent)
    {
        var bone = Spawn();
        _transform.SetParent(bone, parent);
        _transform.SetWorldPosition(bone, boneCompound.Position);
        _transform.SetWorldRotation(bone, boneCompound.Rotation);
        var boneComp = AddComp<BoneComponent>(bone);
        
        if(boneCompound.Data is not null)
         boneComp.BoneVertexDatum = boneCompound.Data;

        if (boneCompound.Child is null) 
            return bone;
        
        foreach (var child in boneCompound.Child)
        {
            boneComp.Childs.Add(CreateBone(child, bone));
        }

        return bone;
    }
    
    private void ProceedBone(Entity<BoneComponent?> entity, MeshRender mesh, EntityUid uid)
    {
        if(!Resolve(entity, ref entity.Comp))
            return;

        var boneTransform = Comp<Transform3dComponent>(entity);
        var ownerTransform = Comp<Transform3dComponent>(uid);

        foreach (var data in entity.Comp.BoneVertexDatum)
        {
            var matrix =
                Matrix4Helpers.CreateTranslation(entity.Comp.OriginalPosition);
            

            mesh.TranslatedVertexes[data.BoneIndices] -= entity.Comp.OriginalPosition;
            mesh.TranslatedVertexes[data.BoneIndices] =
                (boneTransform.WorldRotation - entity.Comp.OriginalRotation).RotateVec(mesh.TranslatedVertexes[data.BoneIndices]);

            mesh.TranslatedVertexes[data.BoneIndices] += entity.Comp.OriginalPosition;
            
            mesh.TranslatedVertexes[data.BoneIndices] +=
                (boneTransform.WorldPosition - entity.Comp.OriginalPosition) * data.BoneWeights;
        }

        foreach (var child in entity.Comp.Childs)
        {
            ProceedBone(child, mesh, uid);
        }
    }
    
    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SkeletonComponent, ModelComponent>();
        while (query.MoveNext(out var uid, out var skeleton, out var model))
        {
            if(!model.MeshRenderInitialized) return;
            
            ProceedBone(skeleton.Root, model.MeshRender, uid);
        }
    }
}