using System.Diagnostics.CodeAnalysis;
using Content.Shared.Transform;

namespace Content.Shared.Camera;

public sealed class CameraSystem : EntitySystem
{
    [Dependency] private readonly Transform3dSystem _transform3dSystem = default!;
     
    public override void Initialize()
    {
        SubscribeLocalEvent<FollowCameraCreateComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<FollowCameraCreateComponent> ent, ref ComponentInit args)
    {
        var camUid = Spawn();
        EnsureComp<CameraComponent>(camUid);
        EnsureComp<CameraProxyComponent>(ent).CameraUid = camUid;
        var followComp = EnsureComp<FollowCameraComponent>(camUid);
        followComp.EntityUid = ent;
        followComp.Offset = ent.Comp.Offset;
        followComp.Rotation = ent.Comp.Rotation;
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
            var entTransform = Comp<Transform3dComponent>(followComp.EntityUid);
            if(transform3DComponent.ParentUid != entTransform.ParentUid)
            {
                _transform3dSystem.SetParent(uid, entTransform.ParentUid);
                transform3DComponent.LocalAngle = followComp.Rotation;
            }
            
            var moveTo = entTransform.LocalPosition + followComp.Offset;
            var moveFrom = transform3DComponent.LocalPosition;
            
            var delta = moveFrom - moveTo;
            
            transform3DComponent.LocalPosition -= delta;
        }
    }
}