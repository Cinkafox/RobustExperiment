using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Transform;

[RegisterComponent, NetworkedComponent]
public sealed partial class Transform3dComponent: Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    
    [DataField("parent")] private EntityUid _parent;

    [DataField("pos")] private Vector3 _localPosition = Vector3.Zero;

    [DataField("rot")] private Quaternion _localRotation = Quaternion.Identity;
    [DataField("scale")] private Vector3 _localScale = Vector3.One;

    [ViewVariables] private HashSet<EntityUid> _children = new();

    private bool MatricesDirty { get; set; }
    private Matrix4x4 _localMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invLocalMatrix = Matrix4x4.Identity;

    public EntityUid ParentUid
    {
        get => _parent;
        set
        {
            if(_parent.IsValid())
                _entMan.GetComponent<Transform3dComponent>(_parent)._children.Remove(Owner);
            _parent = value;
            
            if(_parent.IsValid())
            {
                _entMan.GetComponent<Transform3dComponent>(_parent)._children.Add(Owner);
                IsRooted = false;
            }
            else
            {
                IsRooted = true;
            }
        }
    }

    internal bool IsRooted { get; private set; } = true;

    private void MarkDirtyRecursive()
    {
        if (MatricesDirty)
            return;

        MatricesDirty = true;

        foreach (var child in _children)
        {
            _entMan.GetComponent<Transform3dComponent>(child).MarkDirtyRecursive();
        }
    }

    public void RebuildMatrices()
    {
        MatricesDirty = false;

        if (IsRooted)
        {
            _localMatrix = Matrix4x4.Identity;
            _invLocalMatrix = Matrix4x4.Identity;
            return;
        }

        var rotation = Quaternion.Normalize(_localRotation);

        _localMatrix = Matrix4Helpers.CreateTransform(_localPosition, rotation, _localScale);
        _invLocalMatrix = Matrix4Helpers.CreateInverseTransform(_localPosition, rotation, _localScale);
    }
}

