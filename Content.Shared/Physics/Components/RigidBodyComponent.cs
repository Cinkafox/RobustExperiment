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
    
    [DataField] public float StaticFriction = 0.8f;
    [DataField] public float DynamicFriction = 0.6f;
    [DataField] public float RollingResistance = 0.005f;
    [DataField] public ProtoId<SurfacePrototype> SurfaceMaterial = "default";
    
    [ViewVariables(VVAccess.ReadOnly)] public float Mass => Shape.Area * Density;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 LinearForce => LinearVelocity * Mass;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 AngularForce => AngularVelocity * Mass;
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