using Robust.Shared.Physics.Controllers;

namespace Content.Shared.Movement;

public sealed partial class SharedMoverController : VirtualController
{
    private EntityQuery<InputMoverComponent> _moverQuery;
    public override void Initialize()
    { 
        _moverQuery = GetEntityQuery<InputMoverComponent>();
        InitializeInput();
    }
}
