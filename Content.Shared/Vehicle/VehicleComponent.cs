namespace Content.Shared.Vehicle;

[RegisterComponent]
public sealed partial class VehicleComponent : Component
{
    [DataField] public double MaxPower = 0.55;
    [DataField] public double MaxReverse = 0.375;
    [DataField] public double PowerFactor = 0.1;
    [DataField] public double ReverseFactor = 0.01;
    [DataField] public double BreakFactor = 1;

    [DataField] public double Drag = 0.95;
    [DataField] public double AngularDrag = 0.95;

    [DataField] public double TurnOnBreakFactor = 0.25;
    [DataField] public double TurnSpeedFactor = 0.02;
    [DataField] public double TurnMax = 0.25; 

    [ViewVariables] public double Turn = 0;
    [ViewVariables] public double Power = 0;
    [ViewVariables] public double Reverse = 0;
    [ViewVariables] public double AngularVelocity = 0;
    public double CurrentSpeed => Power - Reverse;
}