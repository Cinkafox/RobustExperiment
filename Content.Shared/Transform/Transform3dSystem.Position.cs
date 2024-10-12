using Content.Shared.Utils;
using Robust.Shared.Utility;

namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem
{
    private void SetWorldPositionRotationInternal(EntityUid uid, Vector3 worldPos, EulerAngles? worldRot = null, Transform3dComponent? component = null)
    {
        if (!_xformQuery.Resolve(uid, ref component))
            return;

        // If no worldRot supplied then default the new rotation to 0.

        if (!component.ParentUid.IsValid())
        {
            DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
            return;
        }

        component.WorldPosition = worldPos;
        
        if (worldRot.HasValue)
        {
            component.WorldAngle = worldRot.Value;
        }
    }

    private (Vector3 pos, EulerAngles rot) GetWorldPositionRotation(Transform3dComponent xform, EntityQuery<Transform3dComponent> xformQuery)
    {
        return (xform.WorldPosition, xform.WorldAngle);
    }

    public void SetWorldPosition(EntityUid uid, Vector3 position)
    {
        _xformQuery.Comp(uid).WorldPosition = position;
    }
    
    public void SetWorldRotation(EntityUid uid, EulerAngles angles)
    {
        _xformQuery.Comp(uid).WorldAngle = angles;
    }
}