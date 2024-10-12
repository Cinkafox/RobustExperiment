using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.Animations;
using Robust.Shared.Utility;
namespace Content.Shared.Transform;

public partial class Transform3dComponent
{
    
    /// <summary>
    ///     Current world rotation of the entity.
    /// </summary>
    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector3 WorldScale
    {
        get
        {
            var parent = _parent;
            var xformQuery = _entMan.GetEntityQuery<Transform3dComponent>();
            var scale = _localScale;

            while (parent.IsValid())
            {
                var parentXform = xformQuery.GetComponent(parent);
                scale *= parentXform._localScale;
                parent = parentXform.ParentUid;
            }

            return scale;
        }
        
        set
        {
            var current = WorldScale;
            var diff = value - current;
            LocalScale *= diff;
        }
    }
    
    [ViewVariables(VVAccess.ReadWrite)]
    [Animatable]
    public EulerAngles WorldAngle
    {
        get => WorldRotation.ToEulerAngle();
        set => WorldRotation = value.ToQuaternion();
    }
    
    /// <summary>
    ///     Current world rotation of the entity.
    /// </summary>
    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Quaternion WorldRotation
    {
        get
        {
            var parent = _parent;
            var xformQuery = _entMan.GetEntityQuery<Transform3dComponent>();
            var rotation = _localRotation;

            while (parent.IsValid())
            {
                var parentXform = xformQuery.GetComponent(parent);
                rotation = Quaternion.Multiply(parentXform._localRotation, Quaternion.Inverse(rotation));
                parent = parentXform.ParentUid;
            }

            return rotation;
        }
        set
        {
            var current = WorldRotation;
            var diff = value * Quaternion.Inverse(current);
            LocalRotation = Quaternion.Multiply(diff, LocalRotation);
        }
    }
    
    /// <summary>
    ///     Current position offset of the entity relative to the world.
    ///     Can de-parent from its parent if the parent is a grid.
    /// </summary>
    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector3 WorldPosition
    {
        get
        {
            if (!_isRooted)
            {
                // parent coords to world coords
                return Vector3.Transform(_localPosition, _entMan.GetComponent<Transform3dComponent>(ParentUid).WorldMatrix);
            }

            return Vector3.Zero;
        }
        set
        {
            if (_isRooted)
            {
                DebugTools.Assert("Parent is invalid while attempting to set WorldPosition - did you try to move root node?");
                return;
            }

            // world coords to parent coords
            var newPos = Vector3.Transform(value, _entMan.GetComponent<Transform3dComponent>(ParentUid).InvWorldMatrix);

            LocalPosition = newPos;
        }
    }
    
    /// <summary>
    ///     Matrix for transforming points from local to world space.
    /// </summary>
    public Matrix4x4 WorldMatrix
    {
        get
        {
            var xformQuery = _entMan.GetEntityQuery<Transform3dComponent>();
            var parent = _parent;
            var myMatrix = LocalMatrix;

            while (parent.IsValid())
            {
                var parentXform = xformQuery.GetComponent(parent);
                var parentMatrix = parentXform.LocalMatrix;
                parent = parentXform.ParentUid;

                var result = Matrix4x4.Multiply(myMatrix, parentMatrix);
                myMatrix = result;
            }

            return myMatrix;
        }
    }

    /// <summary>
    ///     Matrix for transforming points from world to local space.
    /// </summary>
    public Matrix4x4 InvWorldMatrix
    {
        get
        {
            var xformQuery = _entMan.GetEntityQuery<Transform3dComponent>();
            var parent = _parent;
            var myMatrix = InvLocalMatrix;

            while (parent.IsValid())
            {
                var parentXform = xformQuery.GetComponent(parent);
                var parentMatrix = parentXform.InvLocalMatrix;
                parent = parentXform.ParentUid;

                var result = Matrix4x4.Multiply(parentMatrix, myMatrix);
                myMatrix = result;
            }

            return myMatrix;
        }
    }
}