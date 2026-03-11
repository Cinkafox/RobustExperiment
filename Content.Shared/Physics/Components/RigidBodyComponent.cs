using Content.Shared.Physics.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics.Components;

[RegisterComponent]
public sealed partial class RigidBodyComponent: Component
{
    [DataField] public Vector3 AngularVelocity = Vector3.Zero;
    [DataField] public Vector3 LinearVelocity = Vector3.Zero;
    [DataField] public float Density = 1f;
    [DataField] public PhysType PhysType = PhysType.Dynamic;

    [DataField] public Shapes.IPhysicShape Shape = new Shapes.SphereShape();
    
    [DataField] public float Friction = 0.8f;
    [DataField] public float Restitution = 0.3f;
    [DataField] public float RollingResistance = 0.015f;
    
    [ViewVariables(VVAccess.ReadOnly)] public float Mass => Shape.Area * Density;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 LinearForce => LinearVelocity * Mass;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 AngularForce => AngularVelocity * Mass;
    
    [ViewVariables(VVAccess.ReadOnly)] public bool IsGrounded { get; private set; }
    [ViewVariables(VVAccess.ReadOnly)] public int GroundContactCount { get; private set; }
    [ViewVariables(VVAccess.ReadOnly)] public float GroundNormalY { get; private set; }
    
    public void ResetGroundState()
    {
        IsGrounded = false;
        GroundContactCount = 0;
        GroundNormalY = 0f;
    }
    
    public void AddGroundContact(float normalY)
    {
        GroundContactCount++;
        GroundNormalY = MathF.Max(GroundNormalY, normalY);
        IsGrounded = true;
    }
}

[Serializable, NetSerializable]
public enum PhysType
{
    Dynamic, Static,
}

[Prototype]
public sealed partial class SurfacePrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public FrictionData DefaultFriction;
    [DataField] public Dictionary<ProtoId<SurfacePrototype>, FrictionData> MaterialPairs = [];
}

[DataDefinition]
public partial struct FrictionData
{
    [DataField("static")] public float StaticFriction = 0.8f;
    [DataField("dynamic")] public float DynamicFriction = 0.6f;

    public FrictionData(float staticFriction, float dynamicFriction)
    {
        StaticFriction = staticFriction;
        DynamicFriction = dynamicFriction;
    }
}