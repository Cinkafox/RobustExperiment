using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Shared.Transform;

[RegisterComponent, NetworkedComponent]
public sealed partial class Transform3dComponent: Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    
    [DataField("parent")] internal EntityUid _parent;

    [DataField("pos")] internal Vector3 _localPosition;

    [DataField("rot")] internal Angle3d _localRotation;
    [DataField("scale")] internal Vector3 _localScale = Vector3.One;

    [ViewVariables] internal HashSet<EntityUid> _children = new HashSet<EntityUid>();
    
    public bool MatricesDirty { get; private set; }
    private Matrix4 _localMatrix = Matrix4.Identity;
    private Matrix4 _invLocalMatrix = Matrix4.Identity;
    public EntityUid ParentUid  => _parent;

    internal bool _isRooted = true;
    
    public Matrix4 LocalMatrix
    {
        get
        {
            if (MatricesDirty)
                RebuildMatrices();
            
            return _localMatrix;
        }
    }
    public Matrix4 InvLocalMatrix
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
    public Angle3d LocalRotation
    {
        get => _localRotation;
        set
        {
            if(_isRooted)
                return;

            if (_localRotation.EqualsApprox(value))
                return;
            
            _localRotation = value;
            MatricesDirty = true;
        }
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

    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector3 WorldRotAnim
    {
        get
        {
            var rot = WorldRotation;
            return new Vector3((float)rot.Pitch, (float)rot.Yaw, (float)rot.Roll);
        }

        set => WorldRotation = new Angle3d(value.X, value.Y, value.Z);
    }
    
    /// <summary>
    ///     Current world rotation of the entity.
    /// </summary>
    [Animatable]
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle3d WorldRotation
    {
        get
        {
            var parent = _parent;
            var xformQuery = _entMan.GetEntityQuery<Transform3dComponent>();
            var rotation = _localRotation;

            while (parent.IsValid())
            {
                var parentXform = xformQuery.GetComponent(parent);
                rotation += parentXform._localRotation;
                parent = parentXform.ParentUid;
            }

            return rotation;
        }
        set
        {
            var current = WorldRotation;
            var diff = value - current;
            LocalRotation += diff;
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
            else
            {
                return Vector3.Zero;
            }
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
    public Matrix4 WorldMatrix
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

                var result = Matrix4.Mult(myMatrix, parentMatrix);
                myMatrix = result;
            }

            return myMatrix;
        }
    }

    /// <summary>
    ///     Matrix for transforming points from world to local space.
    /// </summary>
    [Obsolete("Use the system method instead")]
    public Matrix4 InvWorldMatrix
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

                var result = Matrix4.Mult(parentMatrix, myMatrix);
                myMatrix = result;
            }

            return myMatrix;
        }
    }
    
    public void RebuildMatrices()
    {
        MatricesDirty = false;

        if (_isRooted) // Root Node
        {
            _localMatrix = Matrix4.Identity;
            _invLocalMatrix = Matrix4.Identity;
        }

        _localMatrix = Matrix4Helpers.CreateTransform(_localPosition, _localRotation, _localScale);
        _invLocalMatrix = Matrix4Helpers.CreateInverseTransform(_localPosition, _localRotation, _localScale);
    }

    [Obsolete("Use TransformSystem.SetParent() instead")]
    public void AttachParent(EntityUid parent)
    {
        _entMan.EntitySysManager.GetEntitySystem<Transform3dSystem>().SetParent(Owner, this, parent, _entMan.GetEntityQuery<Transform3dComponent>());
    }
}

