using System.Numerics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Movement;

public sealed partial class SharedMoverController : VirtualController
{
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    
    protected EntityQuery<InputMoverComponent> MoverQuery;
    public override void Initialize()
    { 
        MoverQuery = GetEntityQuery<InputMoverComponent>();
        InitializeInput();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<InputMoverComponent>();
        while (query.MoveNext(out var uid, out var inputMoverComponent))
        {
            switch (inputMoverComponent.PushedButtons)
            {
                case MoveButtons.Up:
                    PhysicsSystem.ApplyForce(uid,new Vector2(0,1));
                    break;
                case MoveButtons.Down:
                    PhysicsSystem.ApplyForce(uid,new Vector2(0,-1));
                    break;
                case MoveButtons.Left:
                    PhysicsSystem.ApplyForce(uid,new Vector2(1,0));
                    break;
                case MoveButtons.Right:
                    PhysicsSystem.ApplyForce(uid,new Vector2(-1,0));
                    break;
            }
        }
    }
}
