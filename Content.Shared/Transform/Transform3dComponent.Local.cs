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
            if(_isRooted)
                return;
            
            if(_localScale.Equals(value))
                return;

            _localScale = value;
            MatricesDirty = true;
        }
    }
    
    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            if(_isRooted)
                return;

            if (_localRotation.Equals(value))
                return;
            
            _localRotation = value;
            MatricesDirty = true;
        }
    }
    
    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public Robust.Shared.Maths.Vector3 LocalAngleAnim
    {
        get
        {
            var angle = LocalRotation.ToEulerAngle();
            return new Robust.Shared.Maths.Vector3((float)angle.Pitch, (float)angle.Yaw, (float)angle.Roll);
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
            MatricesDirty = true;
        }
    }
}