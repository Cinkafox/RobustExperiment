using Content.Shared.Utils;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;

namespace Content.Shared.Transform;

[RegisterComponent, NetworkedComponent]
public sealed partial class Transform3dComponent: Component
{
    [DataField("parent")] private EntityUid _parent;

    [DataField("pos")] private Vector3 _localPosition;

    [DataField("rot")] private Angle3d _localRotation;
    [DataField("scale")] private Vector3 _localScale = Vector3.One;

    [ViewVariables] private HashSet<EntityUid> _children = new HashSet<EntityUid>();
    
    public bool MatricesDirty { get; private set; }
    private Matrix4 _localMatrix { get; set; }
    private Matrix4 _invLocalMatrix { get; set; }

    private bool _isRooted = true;
    
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

    public void AddChild(Entity<Transform3dComponent> child)
    {
        if(_children.Add(child))
            child.Comp._isRooted = false;
    }

    public void RemoveChild(Entity<Transform3dComponent> child)
    {
        if (_children.Remove(child))
            child.Comp._isRooted = true;
    }
}

