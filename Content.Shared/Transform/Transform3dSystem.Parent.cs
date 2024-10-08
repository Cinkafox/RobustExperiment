using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem
{
    public Transform3dComponent? GetParent(EntityUid uid)
    {
        return GetParent(XformQuery.GetComponent(uid));
    }

    public Transform3dComponent? GetParent(Transform3dComponent xform)
    {
        if (!xform.ParentUid.IsValid())
            return null;
        return XformQuery.GetComponent(xform.ParentUid);
    }

    public EntityUid GetParentUid(EntityUid uid)
    {
        return XformQuery.GetComponent(uid).ParentUid;
    }

    public void SetParent(EntityUid uid, EntityUid parent)
    {
        SetParent(uid, XformQuery.GetComponent(uid), parent, XformQuery);
    }

    public void SetParent(EntityUid uid, Transform3dComponent xform, EntityUid parent, Transform3dComponent? parentXform = null)
    {
        SetParent(uid, xform, parent, XformQuery, parentXform);
    }

    public void SetParent(EntityUid uid, Transform3dComponent xform, EntityUid parent, EntityQuery<Transform3dComponent> xformQuery, Transform3dComponent? parentXform = null)
    {
        DebugTools.AssertOwner(uid, xform);
        if (xform.ParentUid == parent)
            return;

        if (!parent.IsValid())
        {
            DetachEntity(uid, xform);
            return;
        }

        if (!xformQuery.Resolve(parent, ref parentXform))
            return;

        var (_, parRot, parInvMatrix) = GetWorldPositionRotationInvMatrix(parentXform, xformQuery);
        var (pos, rot) = GetWorldPositionRotation(xform, xformQuery);
        var newPos = Vector3.Transform(pos, parInvMatrix);
        var newRot = rot - parRot;

        parentXform._children.Add(uid);
        xform._parent = parent;
        xform._isRooted = false;
        
        SetWorldPositionRotationInternal(uid, newPos, newRot);
    }
    
    public void DetachEntity(EntityUid uid, Transform3dComponent xform)
    {
        XformQuery.TryGetComponent(xform.ParentUid, out var oldXform);
        DetachEntity(uid, xform, MetaData(uid), oldXform);
    }

   
    public void DetachEntity(
            EntityUid uid,
            Transform3dComponent xform,
            MetaDataComponent meta,
            Transform3dComponent? oldXform,
            bool terminating = false)
        {
            
        }
}