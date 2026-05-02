using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.Animations;

namespace Content.Shared.Transform;

public partial class Transform3dComponent
{
    public Matrix4x4 LocalMatrix
    {
        get
        {
            if (MatricesDirty)
                RebuildMatrices();
            
            return _localMatrix;
        }
    }
    public Matrix4x4 InvLocalMatrix
    {
        get
        {
            if (MatricesDirty)
                RebuildMatrices();
            return _invLocalMatrix;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            if(IsRooted)
                return;
            
            if (Vector3.DistanceSquared(_localScale, value) < 1e-8f)
                return;

            _localScale = value;
            MarkDirtyRecursive();
        }
    }
    
    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            if(IsRooted)
                return;

            if (value.Equals(_localRotation))
                return;
            
            _localRotation = Quaternion.Normalize(value);
            MarkDirtyRecursive();
        }
    }
    
    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public Vector3 LocalAngleAnim
    {
        get
        {
            var angle = LocalRotation.ToEulerAngle();
            return new Vector3((float)angle.Pitch, (float)angle.Yaw, (float)angle.Roll);
        }
        set => LocalRotation = new EulerAngles(value.X, value.Y, value.Z).ToQuaternion();
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public EulerAngles LocalAngle
    {
        get => LocalRotation.ToEulerAngle();
        set => LocalRotation = value.ToQuaternion();
    }
    
    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            if (_localPosition.Equals(value))
                return;

            _localPosition = value;
            MarkDirtyRecursive();
        }
    }
}