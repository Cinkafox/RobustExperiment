using System.Numerics;
using Content.Shared.Utils;
using Robust.Shared.GameStates;

namespace Content.Shared.Transform;

[RegisterComponent, NetworkedComponent]
public sealed partial class Transform3dComponent: Component
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    
    [DataField("parent")] internal EntityUid _parent;

    [DataField("pos")] internal Vector3 _localPosition = Vector3.Zero;

    [DataField("rot")] internal Quaternion _localRotation = Quaternion.Identity;
    [DataField("scale")] internal Vector3 _localScale = Vector3.One;

    [ViewVariables] internal HashSet<EntityUid> _children = new HashSet<EntityUid>();
    
    public bool MatricesDirty { get; private set; }
    private Matrix4x4 _localMatrix = Matrix4x4.Identity;
    private Matrix4x4 _invLocalMatrix = Matrix4x4.Identity;
    public EntityUid ParentUid  => _parent;

    internal bool _isRooted = true;
    
    public void RebuildMatrices()
    {
        MatricesDirty = false;

        if (_isRooted) // Root Node
        {
            _localMatrix = Matrix4x4.Identity;
            _invLocalMatrix = Matrix4x4.Identity;
        }
        
        _localMatrix = Matrix4Helpers.CreateTransform(_localPosition, _localRotation, _localScale);
        _invLocalMatrix = Matrix4Helpers.CreateInverseTransform(_localPosition, _localRotation, _localScale);
    }
}

