using Content.Shared.Movement;

namespace Content.Shared.Vehicle;

public partial class VehicleSystem
{
    public void UpdateControl(float frameTime)
    {
        var query = EntityQueryEnumerator<InputMoverComponent,VehicleComponent>();
        while (query.MoveNext(out var inputMoverComponent,out var vehicleComponent))
        {
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Up))
            {
                vehicleComponent.Power += vehicleComponent.PowerFactor;
            }
            else
            {
                vehicleComponent.Power -= vehicleComponent.PowerFactor;
            }
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Down))
            {
                vehicleComponent.Reverse += vehicleComponent.ReverseFactor;
            }
            else
            {
                vehicleComponent.Reverse -= vehicleComponent.ReverseFactor;
            }

            vehicleComponent.Power = double.Max(vehicleComponent.Power, 0);
            vehicleComponent.Reverse = double.Max(vehicleComponent.Reverse, 0);

            var direction = vehicleComponent.Power > vehicleComponent.Reverse ? 1 : -1;
            
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Left))
            {
                vehicleComponent.AngularVelocity -= direction * vehicleComponent.TurnSpeed;
            }
            if (inputMoverComponent.PushedButtons.HasFlag(MoveButtons.Right))
            {
                vehicleComponent.AngularVelocity += direction * vehicleComponent.TurnSpeed;
            }
        }
    }
}