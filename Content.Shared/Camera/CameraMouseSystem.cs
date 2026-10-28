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
    }

    private void OnMouseMove(EntityUid uid, MouseCameraMoveComponent component, ref OnMouseMoveEvent args)
    {
        if(!TryComp<CameraProxyComponent>(uid, out var proxy))
            return;
        
        var camPitch = component.CameraRotation.Pitch + args.Delta.Y;
        var camYaw = component.CameraRotation.Yaw - args.Delta.X;
        
        camPitch = MathHelper.Clamp(camPitch, -MaxClampValue, MaxClampValue);
        
        component.CameraRotation = new EulerAngles(camPitch, camYaw, 0);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var query = EntityQueryEnumerator<MouseCameraMoveComponent, Transform3dComponent, InputMoverComponent, CameraProxyComponent>();

        while (query.MoveNext(out var uid, out var mouseCameraMove, out var transform, out var inputMover, out var proxy))
        {
            var proxyTransform = Comp<Transform3dComponent>(proxy.FollowUid);
            if (inputMover.IsMoving)
            {
                var deltaYaw = -mouseCameraMove.CameraRotation.Yaw;

                mouseCameraMove.CameraRotation = new EulerAngles(mouseCameraMove.CameraRotation.Pitch,
                    mouseCameraMove.CameraRotation.Yaw + deltaYaw, 0);
                transform.LocalAngle = new EulerAngles(transform.LocalAngle.Pitch, transform.LocalAngle.Yaw - deltaYaw,
                    transform.LocalAngle.Roll);
            }
            else
            {
                var deltaAngle = -mouseCameraMove.CameraRotation.Yaw;

                if (deltaAngle > mouseCameraMove.AutoTurnThreshold || deltaAngle < -mouseCameraMove.AutoTurnThreshold)
                {
                    deltaAngle *= 0.15;
                    mouseCameraMove.CameraRotation = new EulerAngles(mouseCameraMove.CameraRotation.Pitch,
                        mouseCameraMove.CameraRotation.Yaw + deltaAngle, 0);
                    transform.LocalAngle = new EulerAngles(transform.LocalAngle.Pitch,
                        transform.LocalAngle.Yaw - deltaAngle, transform.LocalAngle.Roll);
                }

                proxyTransform.LocalAngle = mouseCameraMove.CameraRotation;
            }


            proxyTransform.LocalAngle = mouseCameraMove.CameraRotation;
        }
        
    }
}