namespace Content.Shared.Vehicle;

[RegisterComponent]
public sealed partial class VehicleComponent : Component
{
    [DataField] public double MaxPower = 0.75;
    [DataField] public double MaxReverse = 0.375;
    [DataField] public double PowerFactor = 0.1;
    [DataField] public double ReverseFactor = 0.01;

    [DataField] public double Drag = 0.95;
    [DataField] public double AngularDrag = 0.95;
    [DataField] public double TurnSpeed = 0.2;
    
    [ViewVariables] public double Power = 0;
    [ViewVariables] public double Reverse = 0;
    [ViewVariables] public double AngularVelocity = 0;
}