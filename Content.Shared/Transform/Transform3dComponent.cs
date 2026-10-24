using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Transform;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class Transform3dComponent : Component, IComponentDelta
{
    [Dependency] private IEntityManager _entMan = default!;

    [DataField("parent")] private EntityUid _parent;

    [DataField("pos")] private Vector3 _localPosition = Vector3.Zero;

    [DataField("rot")] private Quaternion _localRotation = Quaternion.Identity;
    [DataField("scale")] private Vector3 _localScale = Vector3.One;

    [ViewVariables] private HashSet<EntityUid> _children = new();

    private bool MatricesDirty { get; set; }
    private Matrix4x4 _localMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invLocalMatrix = Matrix4x4.Identity;

    [ViewVariables, AutoNetworkedField]
    public EntityUid ParentUid
    {
        get => _parent;
        set
        {
            if (_parent == value)
                return;

            if (_parent.IsValid())
            {
                if (_entMan.TryGetComponent<Transform3dComponent>(_parent, out var oldParent))
                    oldParent._children.Remove(Owner);
            }

            _parent = value;

            if (_parent.IsValid())
            {
                if (_entMan.TryGetComponent<Transform3dComponent>(_parent, out var newParent))
                    newParent._children.Add(Owner);
                IsRooted = false;
            }
            else
            {
                IsRooted = true;
            }

            if (_entMan.TryGetComponent<MetaDataComponent>(Owner, out var meta))
                _entMan.DirtyField(Owner, this, nameof(ParentUid), meta);
        }
    }

    internal bool IsRooted { get; private set; } = true;

    private void MarkDirtyRecursive(string? fieldName)
    {
        if (MatricesDirty)
            return;

        MatricesDirty = true;
        
        if(fieldName != null)
            _entMan.DirtyField(Owner, this, fieldName);

        foreach (var child in _children)
        {
            _entMan.GetComponent<Transform3dComponent>(child).MarkDirtyRecursive(fieldName);
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

    GameTick IComponentDelta.LastUnclassifiedDirty { get; set; }
    GameTick[] IComponentDelta.LastModifiedFields { get; set; } = default!;
}

