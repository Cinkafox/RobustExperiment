using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Shared.Camera;

public sealed partial class CameraSystem : EntitySystem
{
    [Dependency] private Transform3dSystem _transform3dSystem = default!;
     
    public override void Initialize()
    {
        SubscribeLocalEvent<FollowCameraCreateComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<FollowCameraCreateComponent> ent, ref ComponentInit args)
    {
        var camUid = Spawn();
        EnsureComp<CameraComponent>(camUid);
        var camProxComp = EnsureComp<CameraProxyComponent>(ent);
        camProxComp.CameraUid = camUid;
        //Dirty(ent, camProxComp);
        var followComp = EnsureComp<FollowCameraComponent>(camUid);
        followComp.CameraQueue.Add(ent, new FollowCameraEntry()
        {
            Offset = ent.Comp.Offset,
            Rotation = ent.Comp.Rotation,
        });
    }

    public void AddFollowEntity(EntityUid uid, EntityUid follow, Vector3 offset, EulerAngles rotation, bool rotateByEntity = false)
    {
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