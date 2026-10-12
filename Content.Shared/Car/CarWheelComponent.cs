namespace Content.Shared.Car;

[RegisterComponent]
public sealed partial class CarWheelComponent : Component
{
    [DataField] public Vector3 Direction;
    [DataField] public float Speed;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 Velocity => Direction * Speed;
}

[RegisterComponent]
public sealed partial class CarWheelSteeringComponent : Component
{
    [DataField] public Angle SteeringAngle = Angle.FromDegrees(30);
}

[RegisterComponent]
public sealed partial class CarComponent : Component
{
    [ViewVariables] public readonly List<EntityUid> Steering = [];
    [ViewVariables] public readonly List<EntityUid> Wheels = [];
}

[RegisterComponent]
public sealed partial class CarRemoteControllerComponent : Component
{
}

[RegisterComponent]
public sealed partial class PlayerActiveCarRemoteControlComponent : Component
{
    [ViewVariables] public EntityUid Car;
}