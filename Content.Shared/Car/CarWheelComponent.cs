using Robust.Shared.GameStates;

namespace Content.Shared.Car;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CarWheelComponent : Component
{
    [DataField, AutoNetworkedField] public Vector3 Direction;
    [DataField, AutoNetworkedField] public float Speed;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 Velocity => Direction * Speed;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CarWheelSteeringComponent : Component
{
    [DataField, AutoNetworkedField] public Angle SteeringAngle = Angle.FromDegrees(30);
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CarComponent : Component
{
    [ViewVariables, AutoNetworkedField] public readonly List<EntityUid> Steering = [];
    [ViewVariables, AutoNetworkedField] public readonly List<EntityUid> Wheels = [];
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CarRemoteControllerComponent : Component
{
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlayerActiveCarRemoteControlComponent : Component
{
    [ViewVariables, AutoNetworkedField] public EntityUid Car;
}