using Content.Client.DimensionEnv;
using Content.Client.DimensionEnv.ObjRes;
using Content.Shared.Bone;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Client.Bone;

public sealed class BoneSkinningSystem : EntitySystem
{
    private void ProceedBone(Entity<BoneComponent?> entity, MeshRender mesh, Transform3dComponent parentTransform)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var boneTransform = Comp<Transform3dComponent>(entity);
        
        var originalQ = entity.Comp.OriginalRotation.ToQuaternion();
        var invOriginalQ = Quaternion.Inverse(originalQ);
        var currentQ = boneTransform.WorldRotation;
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