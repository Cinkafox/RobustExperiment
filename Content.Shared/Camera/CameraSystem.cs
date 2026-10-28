using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Bone;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Timing;

namespace Content.Shared.Camera;

public sealed partial class CameraSystem : EntitySystem
{
    [Dependency] private Transform3dSystem _transform3dSystem = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private BoneSystem _boneSystem = default!;
    
    [SubscribeLocalEvent]
    private void OnInit(Entity<FollowCameraCreateComponent> ent, ref SkeletonInitializedEvent args)
    {
        var camUid = Spawn();
        AddComp<CameraComponent>(camUid);
        
        var followEnt = ent.Owner;

        if (ent.Comp.Bone is not null && 
            _boneSystem.TryGetBone(ent.Owner, ent.Comp.Bone, out var boneUid))
        {
            followEnt = boneUid;
        }
        else
        {
            Log.Error($"Bone {ent.Comp.Bone} not found!");   
            if(!TryComp<SkeletonComponent>(ent.Owner, out var comp))
                Log.Error("No skeleton!");
            else
            {
                Log.Error(comp.BonesDictionary.Count + "<<");
            }
        }
        
        var camProxComp = EnsureComp<CameraProxyComponent>(ent);
        camProxComp.CameraUid = camUid;
        camProxComp.FollowUid = followEnt;
        Dirty(ent, camProxComp);
        var followComp = EnsureComp<FollowCameraComponent>(camUid);
        followComp.CameraQueue.Add(followEnt, new FollowCameraEntry()
        {
            Offset = ent.Comp.Offset,
            Rotation = ent.Comp.Rotation,
            RotateByEntity = ent.Comp.RotateByEntity
        });
        
        Dirty(camUid, followComp);
    }

    public void AddFollowEntity(EntityUid uid, EntityUid follow, Vector3 offset, EulerAngles rotation, bool rotateByEntity = false)
    {
        if(!_gameTiming.IsFirstTimePredicted)
            return;
        
        if (!TryComp<CameraProxyComponent>(uid, out var cameraProxyComponent) ||
            !TryComp<FollowCameraComponent>(cameraProxyComponent.CameraUid, out var followComp))
            return;
        
        RaiseLocalEvent(uid, new FollowEntityEvent()
        {
            FollowBy = follow,
        });

        followComp.CameraQueue.Add(follow, new FollowCameraEntry()
        {
            Offset = offset,
            Rotation = rotation,
            RotateByEntity = rotateByEntity
        });
        
        Dirty(cameraProxyComponent.CameraUid, followComp);
    }

    public void RemoveFollowEntity(EntityUid uid, EntityUid follow)
    {
        if (!TryComp<CameraProxyComponent>(uid, out var cameraProxyComponent) ||
            !TryComp<FollowCameraComponent>(cameraProxyComponent.CameraUid, out var followComp))
            return;
        
        RaiseLocalEvent(uid, new UnFollowEntityEvent()
        {
            FollowBy = follow,
        });
        
        followComp.CameraQueue.Remove(follow);
    }

    public bool TryGetCamera(EntityUid? uid, [NotNullWhen(true)] out Entity<CameraComponent, Transform3dComponent>? camera)
    {
        camera = null;
        
        if (uid is null) 
            return false;
        
        if (TryComp<CameraProxyComponent>(uid, out var cameraProxyComponent))
            return TryGetCamera(cameraProxyComponent.CameraUid, out camera);
        
        if (!TryComp<CameraComponent>(uid, out var cameraComponent) || 
            !TryComp<Transform3dComponent>(uid, out var transform3dComponent))
            return false;

        camera = new Entity<CameraComponent, Transform3dComponent>(uid.Value, cameraComponent, transform3dComponent);
        return true;
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<FollowCameraComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var followComp, out var transform3DComponent))
        {
            if(followComp.CameraQueue.Count < 1) 
                continue;

            var (followEntity, followEntry) = followComp.CameraQueue.Last();
            
            var entTransform = Comp<Transform3dComponent>(followEntity);
            if(transform3DComponent.ParentUid != entTransform.ParentUid)
            {
                _transform3dSystem.SetParent(uid, entTransform.ParentUid);
            }
            
            if (followEntry.RotateByEntity)
            {
                var moveTo = entTransform.LocalPosition + Vector3.Transform(followEntry.Offset, entTransform.LocalRotation);
                var moveFrom = transform3DComponent.LocalPosition;
            
                var delta = moveFrom - moveTo;
                
                transform3DComponent.LocalAngle = followEntry.Rotation;
                transform3DComponent.LocalPosition -= delta;
                transform3DComponent.LocalRotation = entTransform.LocalRotation * followEntry.Rotation.ToQuaternion();
            }
            else
            {
                var moveTo = entTransform.LocalPosition + followEntry.Offset;
                var moveFrom = transform3DComponent.LocalPosition;
            
                var delta = moveFrom - moveTo;
                
                transform3DComponent.LocalAngle = followEntry.Rotation;
                transform3DComponent.LocalPosition -= delta;
            }
        }
    }
}

public sealed class FollowEntityEvent : EntityEventArgs
{
    public EntityUid FollowBy;
}

public sealed class UnFollowEntityEvent : EntityEventArgs
{
    public EntityUid FollowBy;
}