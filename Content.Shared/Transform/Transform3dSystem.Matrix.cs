using System.Runtime.CompilerServices;
using Content.Shared.Utils;
using JetBrains.Annotations;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem
{
    #region GetWorldPositionRotationMatrix
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix)
        GetWorldPositionRotationMatrix(EntityUid uid)
    {
        return GetWorldPositionRotationMatrix(XformQuery.GetComponent(uid), XformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix)
        GetWorldPositionRotationMatrix(Transform3dComponent xform)
    {
        return GetWorldPositionRotationMatrix(xform, XformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix)
        GetWorldPositionRotationMatrix(EntityUid uid, EntityQuery<Transform3dComponent> xforms)
    {
        return GetWorldPositionRotationMatrix(xforms.GetComponent(uid), xforms);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix)
        GetWorldPositionRotationMatrix(Transform3dComponent component, EntityQuery<Transform3dComponent> xforms)
    {
        var (pos, rot) = GetWorldPositionRotation(component, xforms);
        return (pos, rot, Matrix4Helpers.CreateTransform(pos, rot, Vector3.One));
    }
    #endregion

    #region GetWorldPositionRotationInvMatrix

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 InvWorldMatrix) GetWorldPositionRotationInvMatrix(EntityUid uid)
    {
        return GetWorldPositionRotationInvMatrix(XformQuery.GetComponent(uid));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 InvWorldMatrix) GetWorldPositionRotationInvMatrix(Transform3dComponent xform)
    {
        return GetWorldPositionRotationInvMatrix(xform, XformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 InvWorldMatrix) GetWorldPositionRotationInvMatrix(EntityUid uid, EntityQuery<Transform3dComponent> xforms)
    {
        return GetWorldPositionRotationInvMatrix(xforms.GetComponent(uid), xforms);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 InvWorldMatrix) GetWorldPositionRotationInvMatrix(Transform3dComponent component, EntityQuery<Transform3dComponent> xforms)
    {
        var (pos, rot) = GetWorldPositionRotation(component, xforms);
        return (pos, rot, Matrix4Helpers.CreateInverseTransform(pos, rot, Vector3.One));
    }

    #endregion
    
    #region Inverse World Matrix

    
    public Matrix4 GetInvWorldMatrix(EntityUid uid)
    {
        return GetInvWorldMatrix(XformQuery.GetComponent(uid), XformQuery);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 GetInvWorldMatrix(Transform3dComponent component)
    {
        return GetInvWorldMatrix(component, XformQuery);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 GetInvWorldMatrix(EntityUid uid, EntityQuery<Transform3dComponent> xformQuery)
    {
        return GetInvWorldMatrix(xformQuery.GetComponent(uid), xformQuery);
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4 GetInvWorldMatrix(Transform3dComponent component, EntityQuery<Transform3dComponent> xformQuery)
    {
        var (pos, rot) = GetWorldPositionRotation(component, xformQuery);
        return Matrix4Helpers.CreateInverseTransform(pos, rot, Vector3.One);
    }

    #endregion

    #region GetWorldPositionRotationMatrixWithInv

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix, Matrix4 InvWorldMatrix)
        GetWorldPositionRotationMatrixWithInv(EntityUid uid)
    {
        return GetWorldPositionRotationMatrixWithInv(XformQuery.GetComponent(uid), XformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix, Matrix4 InvWorldMatrix)
        GetWorldPositionRotationMatrixWithInv(Transform3dComponent xform)
    {
        return GetWorldPositionRotationMatrixWithInv(xform, XformQuery);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix, Matrix4 InvWorldMatrix)
        GetWorldPositionRotationMatrixWithInv(EntityUid uid, EntityQuery<Transform3dComponent> xforms)
    {
        return GetWorldPositionRotationMatrixWithInv(xforms.GetComponent(uid), xforms);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 WorldPosition, Angle3d WorldRotation, Matrix4 WorldMatrix, Matrix4 InvWorldMatrix)
        GetWorldPositionRotationMatrixWithInv(Transform3dComponent component, EntityQuery<Transform3dComponent> xforms)
    {
        var (pos, rot) = GetWorldPositionRotation(component, xforms);
        return (pos, rot, Matrix4Helpers.CreateTransform(pos, rot, Vector3.One), Matrix4Helpers.CreateInverseTransform(pos, rot, Vector3.One));
    }

    #endregion
}