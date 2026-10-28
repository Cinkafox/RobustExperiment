using Content.Shared.DollAnimation;
using Content.Shared.Mouse;
using Content.Shared.Movement;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Shared.Camera;

public sealed partial class CameraMouseSystem : EntitySystem
{
    public Angle MaxClampValue = Angle.FromDegrees(89);
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<MouseCameraMoveComponent, OnMouseMoveEvent>(OnMouseMove);
        SubscribeLocalEvent<MouseCameraMoveComponent, EntityStartMoveEvent>(OnEntityMove);
    }

    private void OnEntityMove(EntityUid uid, MouseCameraMoveComponent component, EntityStartMoveEvent args)
    {
        
    }

    private void OnMouseMove(EntityUid uid, MouseCameraMoveComponent component, ref OnMouseMoveEvent args)
    {
        if(!TryComp<CameraProxyComponent>(uid, out var proxy))
            return;
        
        var camPitch = component.CameraRotation.Pitch + args.Delta.Y;
        var camYaw = component.CameraRotation.Yaw - args.Delta.X;
        
        camPitch = MathHelper.Clamp(camPitch, -MaxClampValue, MaxClampValue);
        
        component.CameraRotation = new EulerAngles(camPitch, camYaw, 0);
        
        var proxyTransform = Comp<Transform3dComponent>(proxy.FollowUid);
        proxyTransform.LocalAngle = component.CameraRotation;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        
        var query = EntityQueryEnumerator<MouseCameraMoveComponent, Transform3dComponent, InputMoverComponent, CameraProxyComponent>();

        while (query.MoveNext(out var uid, out var mouseCameraMove, out var transform, out var inputMover, out var proxy))
        {
            if(!inputMover.IsMoving) 
                continue;
            
            var proxyTransform = Comp<Transform3dComponent>(proxy.FollowUid);
            
            proxyTransform.LocalAngle = new EulerAngles(proxyTransform.LocalAngle.Pitch, 0, proxyTransform.LocalAngle.Roll);
            transform.LocalAngle =      new EulerAngles(transform.LocalAngle.Pitch, mouseCameraMove.CameraRotation.Yaw, transform.LocalAngle.Roll);
            mouseCameraMove.CameraRotation = proxyTransform.LocalAngle;
        }
        
    }
}