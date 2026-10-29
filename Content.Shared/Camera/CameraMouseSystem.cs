using System.Numerics;
using Content.Shared.Mouse;
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
        if (!TryComp<CameraProxyComponent>(uid, out var proxy))
            return;

        var camPitch = component.CameraRotation.Pitch + args.Delta.Y;
        var camYaw = component.CameraRotation.Yaw - args.Delta.X;

        camPitch = MathHelper.Clamp(camPitch, -MaxClampValue, MaxClampValue);

        component.CameraRotation = new EulerAngles(camPitch, camYaw, 0);
        ApplyRotation(component, proxy);
    }
    
    public EulerAngles? ApplyLook(EntityUid uid, MouseCameraMoveComponent mouse, Vector2 look, float bodyYaw,
        bool updateCameraRotation = true)
    {
        if (!float.IsFinite(look.X) || !float.IsFinite(look.Y) || !float.IsFinite(bodyYaw))
            return null;

        var pitch = MathHelper.Clamp(new Angle(look.Y), -MaxClampValue, MaxClampValue);
        var rotation = new EulerAngles(pitch, new Angle(look.X), 0);

        if (updateCameraRotation)
            mouse.CameraRotation = rotation;

        if (TryComp<Transform3dComponent>(uid, out var transform))
        {
            transform.LocalAngle = new EulerAngles(transform.LocalAngle.Pitch,
                new Angle(bodyYaw), transform.LocalAngle.Roll);
        }

        if (TryComp<CameraProxyComponent>(uid, out var proxy))
            ApplyRotation(rotation, proxy);

        return rotation;
    }

    public void ApplyRotation(EntityUid uid, MouseCameraMoveComponent mouse)
    {
        if (TryComp<CameraProxyComponent>(uid, out var proxy))
            ApplyRotation(mouse, proxy);
    }

    private void ApplyRotation(MouseCameraMoveComponent component, CameraProxyComponent proxy)
    {
        ApplyRotation(component.CameraRotation, proxy);
    }

    private void ApplyRotation(EulerAngles rotation, CameraProxyComponent proxy)
    {
        if (TryComp<Transform3dComponent>(proxy.FollowUid, out var proxyTransform))
            proxyTransform.LocalAngle = rotation;
    }
}
