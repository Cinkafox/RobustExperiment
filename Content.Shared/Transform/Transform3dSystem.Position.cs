using Content.Shared.Utils;
using Robust.Shared.Utility;

namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem
{
    private void SetWorldPositionRotationInternal(EntityUid uid, Vector3 localPos, EulerAngles? localRot = null, Transform3dComponent? component = null)
    {
        if (!_xformQuery.Resolve(uid, ref component))
            return;

        if (!component.ParentUid.IsValid())
        {
            DebugTools.Assert($"Parent is invalid while attempting to set position - did you try to move root node? EntityUid: {uid}");
            return;
        }

        component.LocalPosition = localPos;

        if (localRot.HasValue)
        {
            component.LocalAngle = localRot.Value;
        }
    }

    private (Vector3 pos, EulerAngles rot) GetWorldPositionRotation(Transform3dComponent xform, EntityQuery<Transform3dComponent> xformQuery)
    {
        return (xform.WorldPosition, xform.WorldAngle);
    }

    public void SetWorldPosition(EntityUid uid, Vector3 position)
    {
        var comp = _xformQuery.Comp(uid);
        comp.WorldPosition = position;
    }

    public void SetWorldRotation(EntityUid uid, EulerAngles angles)
    {
        var comp = _xformQuery.Comp(uid);
        comp.WorldAngle = angles;
    }

    public void SetWorldRotation(EntityUid uid, Quaternion orientation)
    {
        var comp = _xformQuery.Comp(uid);
        comp.WorldRotation = orientation;
    }

    public Vector3 GetWorldPosition(EntityUid uid)
    {
        return _xformQuery.Comp(uid).WorldPosition;
    }

    public Quaternion GetWorldRotation(EntityUid uid)
    {
        return _xformQuery.Comp(uid).WorldRotation;
    }
}