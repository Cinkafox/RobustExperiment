using Content.Shared.Movement;

namespace Content.Shared.Vehicle;

public partial class VehicleSystem
{
    public void UpdateControl(float frameTime)
    {
        var query = EntityQueryEnumerator<InputMoverComponent,VehicleComponent>();
        while (query.MoveNext(out var inputMoverComponent,out var vehicleComponent))
        {
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Up) && !inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Walk))
            {
                vehicleComponent.Power += vehicleComponent.PowerFactor;
            }
            else
            {
                vehicleComponent.Power -= vehicleComponent.PowerFactor;
            }
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Down) && !inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Walk))
            {
                vehicleComponent.Reverse += vehicleComponent.ReverseFactor;
            }
            else
            {
                vehicleComponent.Reverse -= vehicleComponent.ReverseFactor;
            }

            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Walk))
            {
                vehicleComponent.Reverse -= vehicleComponent.BreakFactor;
                vehicleComponent.Power -= vehicleComponent.BreakFactor;
                vehicleComponent.Turn = vehicleComponent.TurnOnBreakFactor;
            }
            else
            {
                vehicleComponent.Turn = vehicleComponent.TurnSpeedFactor * (vehicleComponent.Power + vehicleComponent.Reverse);
            }

            vehicleComponent.Turn = double.Clamp(vehicleComponent.Turn, 0, vehicleComponent.TurnMax);
            vehicleComponent.Power = double.Clamp(vehicleComponent.Power, 0, vehicleComponent.MaxPower);
            vehicleComponent.Reverse = double.Clamp(vehicleComponent.Reverse, 0, vehicleComponent.MaxReverse);

            var direction = vehicleComponent.Power >= vehicleComponent.Reverse ? 1 : -1;
            vehicleComponent.Turn = direction * vehicleComponent.Turn;
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Left))
            {
                vehicleComponent.AngularVelocity -= vehicleComponent.Turn;
            }
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Right))
            {
                vehicleComponent.AngularVelocity += vehicleComponent.Turn;
            }
        }
    }
}