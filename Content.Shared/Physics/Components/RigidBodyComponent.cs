using Robust.Shared.Serialization;

namespace Content.Shared.Physics.Components;

[RegisterComponent]
public sealed partial class RigidBodyComponent: Component
{
    [DataField] public Vector3 AngularVelocity = Vector3.Zero;
    [DataField] public Vector3 LinearVelocity = Vector3.Zero;
    [DataField] public float Density = 1f;
    [DataField] public PhysType PhysType = PhysType.Kinematic;

    [DataField] public IPhysicShape Shape = new SphereShape();
    [ViewVariables(VVAccess.ReadOnly)] public float Mass => Shape.Area * Density;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 LinearForce => LinearVelocity * Mass;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 AngularForce => AngularVelocity * Mass;
}

[Serializable, NetSerializable]
public enum PhysType
{
    Kinematic, Static,
}

[ImplicitDataDefinitionForInheritors]
public partial interface IPhysicShape
{
    [ViewVariables(VVAccess.ReadOnly)] public float Area { get; }
}

[DataDefinition]
public sealed partial class SphereShape : IPhysicShape
{
    [DataField] public float Radius = 1f;
    public float Area => 4 * float.Pi * Radius * Radius;
}

[DataDefinition]
public sealed partial class PlaneShape : IPhysicShape
{
    [DataField] public float Distance = 1f;
    [DataField] public Vector3 Normal = Vector3.UnitY;
    public float Area => 1f;
}