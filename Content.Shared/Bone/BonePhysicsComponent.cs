using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Physics.Shapes;

namespace Content.Shared.Bone;

[RegisterComponent]
public sealed partial class BonePhysicsComponent : Component
{
    [DataField] public Dictionary<string, BonePhysicsProperty> BonePhysics = [];
}

[DataDefinition]
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