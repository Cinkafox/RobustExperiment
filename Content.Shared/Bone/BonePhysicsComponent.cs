using Content.Shared.Physics.Components;
using Content.Shared.Physics.Constraints;
using Content.Shared.Physics.Shapes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class BonePhysicsComponent : Component
{
    [DataField, AutoNetworkedField] public Dictionary<string, BonePhysicsProperty> BonePhysics = [];
    [ViewVariables, AutoNetworkedField] public Dictionary<EntityUid, EntityUid> BonesParent = new();
    [ViewVariables] public bool IsBonesInitialized;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BonePhysicsProperty
{
    [DataField] public IPhysicShape Shape = new SphereShape();
    [DataField] public float Density = 1f;
    [DataField] public float Friction = 0.8f;
    [DataField] public float Restitution = 0.3f;
    [DataField] public float RollingResistance = 0.015f;
    [DataField] public bool Takeble = false;

    public PhysicsProperty Property => new()
    {
        Shape =  Shape,
        Density = Density,
        Friction = Friction,
        Restitution = Restitution,
        RollingResistance = RollingResistance
    };

    [DataField] public Dictionary<string, IBodyConstraint> Constraints = [];
}