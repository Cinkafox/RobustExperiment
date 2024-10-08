using System.Numerics;
using System.Runtime.CompilerServices;
using Content.Shared.Utils;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem
{
    #region Local Position

    [Obsolete("use override with EntityUid")]
    public void SetLocalPosition(Transform3dComponent xform, Vector3 value)
    {
        SetLocalPosition(xform.Owner, value, xform);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLocalPosition(EntityUid uid, Vector3 value, Transform3dComponent? xform = null)
        => SetLocalPositionNoLerp(uid, value, xform);


    [Obsolete("use override with EntityUid")]
    public void SetLocalPositionNoLerp(Transform3dComponent xform, Vector3 value)
        => SetLocalPositionNoLerp(xform.Owner, value, xform);

    public void SetLocalPositionNoLerp(EntityUid uid, Vector3 value, Transform3dComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

#pragma warning disable CS0618
        xform.LocalPosition = value;
#pragma warning restore CS0618
    }

    #endregion

    #region Local Rotation

    public void SetLocalRotationNoLerp(EntityUid uid, Angle3d value, Transform3dComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        xform.LocalRotation = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLocalRotation(EntityUid uid, Angle3d value, Transform3dComponent? xform = null)
        => SetLocalRotationNoLerp(uid, value, xform);

    [Obsolete("use override with EntityUid")]
    public void SetLocalRotation(Transform3dComponent xform, Angle3d value)
    {
        SetLocalRotation(xform.Owner, value, xform);
    }

    #endregion
    
    #region World Position

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetWorldPosition(EntityUid uid)
    {
        return GetWorldPosition(XformQuery.GetComponent(uid));
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetWorldPosition(Transform3dComponent component)
    {
        Vector3 pos = component._localPosition;

        while (component.ParentUid.IsValid())
        {
            component = XformQuery.GetComponent(component.ParentUid);
            pos = component._localRotation.RotateVec(pos) + component._localPosition;
        }

        return pos;
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetWorldPosition(EntityUid uid, EntityQuery<Transform3dComponent> xformQuery)
    {
        return GetWorldPosition(xformQuery.GetComponent(uid));
    }

    
    public Vector3 GetWorldPosition(Transform3dComponent component, EntityQuery<Transform3dComponent> xformQuery)
    {
        return GetWorldPosition(component);
    }
    
    
    public (Vector3 WorldPosition, Angle3d WorldRotation) GetWorldPositionRotation(EntityUid uid)
    {
        return GetWorldPositionRotation(XformQuery.GetComponent(uid));
    }

    
    public (Vector3 WorldPosition, Angle3d WorldRotation) GetWorldPositionRotation(Transform3dComponent component)
    {
        Vector3 pos = component._localPosition;
        Angle3d angle = component._localRotation;

        while (component.ParentUid.IsValid())
        {
            component = XformQuery.GetComponent(component.ParentUid);
            pos = component._localRotation.RotateVec(pos) + component._localPosition;
            angle += component._localRotation;
        }

        return (pos, angle);
    }

    
    public (Vector3 WorldPosition, Angle3d WorldRotation) GetWorldPositionRotation(Transform3dComponent component, EntityQuery<Transform3dComponent> xformQuery)
    {
        return GetWorldPositionRotation(component);
    }

    /// <summary>
    ///     Returns the position and rotation relative to some entity higher up in the component's transform hierarchy.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 Position, Angle3d Rotation) GetRelativePositionRotation(
        Transform3dComponent component,
        EntityUid relative,
        EntityQuery<Transform3dComponent> query)
    {
        var rot = component._localRotation;
        var pos = component._localPosition;
        var xform = component;
        while (xform.ParentUid != relative)
        {
            if (xform.ParentUid.IsValid() && query.TryGetComponent(xform.ParentUid, out xform))
            {
                rot += xform._localRotation;
                pos = xform._localRotation.RotateVec(pos) + xform._localPosition;
                continue;
            }

            // Entity was not actually in the transform hierarchy. This is probably a sign that something is wrong, or that the function is being misused.
            Log.Warning($"Target entity ({ToPrettyString(relative)}) not in transform hierarchy while calling {nameof(GetRelativePositionRotation)}.");
            var relXform = query.GetComponent(relative);
            pos = Vector3.Transform(pos, GetInvWorldMatrix(relXform));
            rot = rot - GetWorldRotation(relXform, query);
            break;
        }

        return (pos, rot);
    }

    /// <summary>
    ///     Returns the position and rotation relative to some entity higher up in the component's transform hierarchy.
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetRelativePosition(
        Transform3dComponent component,
        EntityUid relative,
        EntityQuery<Transform3dComponent> query)
    {
        var pos = component._localPosition;
        var xform = component;
        while (xform.ParentUid != relative)
        {
            if (xform.ParentUid.IsValid() && query.TryGetComponent(xform.ParentUid, out xform))
            {
                pos = xform._localRotation.RotateVec(pos) + xform._localPosition;
                continue;
            }

            // Entity was not actually in the transform hierarchy. This is probably a sign that something is wrong, or that the function is being misused.
            Log.Warning($"Target entity ({ToPrettyString(relative)}) not in transform hierarchy while calling {nameof(GetRelativePositionRotation)}.");
            var relXform = query.GetComponent(relative);
            pos = Vector3.Transform(pos, GetInvWorldMatrix(relXform));
            break;
        }

        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(EntityUid uid, Vector3 worldPos)
    {
        var xform = XformQuery.GetComponent(uid);
        SetWorldPosition((uid, xform), worldPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Obsolete("Use overload that takes Entity<T> instead")]
    public void SetWorldPosition(Transform3dComponent component, Vector3 worldPos)
    {
        SetWorldPosition((component.Owner, component), worldPos);
    }

    /// <summary>
    /// Sets the position of the entity in world-terms to the specified position.
    /// May also de-parent the entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldPosition(Entity<Transform3dComponent> entity, Vector3 worldPos)
    {
        SetWorldPositionRotationInternal(entity.Owner, worldPos, null, entity.Comp);
    }

    #endregion
    
    #region World Rotation

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle3d GetWorldRotation(EntityUid uid)
    {
        return GetWorldRotation(XformQuery.GetComponent(uid), XformQuery);
    }

    // Temporary until it's moved here
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle3d GetWorldRotation(Transform3dComponent component)
    {
        return GetWorldRotation(component, XformQuery);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle3d GetWorldRotation(EntityUid uid, EntityQuery<Transform3dComponent> xformQuery)
    {
        return GetWorldRotation(xformQuery.GetComponent(uid), xformQuery);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle3d GetWorldRotation(Transform3dComponent component, EntityQuery<Transform3dComponent> xformQuery)
    {
        Angle3d rotation = component._localRotation;

        while (component.ParentUid.IsValid())
        {
            component = xformQuery.GetComponent(component.ParentUid);
            rotation += component._localRotation;
        }

        return rotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(EntityUid uid, Angle3d angle)
    {
        var component = XformQuery.Comp(uid);
        SetWorldRotation(component, angle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(Transform3dComponent component, Angle3d angle)
    {
        var current = GetWorldRotation(component);
        var diff = angle - current;
        SetLocalRotation(component, component.LocalRotation + diff);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(EntityUid uid, Angle3d angle, EntityQuery<Transform3dComponent> xformQuery)
    {
        SetWorldRotation(xformQuery.GetComponent(uid), angle, xformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWorldRotation(Transform3dComponent component, Angle3d angle, EntityQuery<Transform3dComponent> xformQuery)
    {
        var current = GetWorldRotation(component, xformQuery);
        var diff = angle - current;
        SetLocalRotation(component, component.LocalRotation + diff);
    }
    #endregion
    
    private void SetWorldPositionRotationInternal(EntityUid uid, Vector3 worldPos, Angle3d? worldRot = null, Transform3dComponent? component = null)
    {
        if (!XformQuery.Resolve(uid, ref component))
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
            SetWorldRotation(uid, worldRot.Value);
        }
    }
}