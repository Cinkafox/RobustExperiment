using System.Numerics;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Movement;

public sealed partial class SharedMoverController : VirtualController
{
    [Dependency] protected readonly SharedPhysicsSystem PhysicsSystem = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;
    
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
            var dir = inputMoverComponent.PushedButtons.ToDir();
            if (dir is Direction.Invalid)
            {
                PhysicsSystem.SetLinearVelocity(uid, Vector2.Zero);
                continue;
            }

            var angle = dir.ToAngle() + Angle.FromDegrees(270);
            PhysicsSystem.SetLinearVelocity(uid, angle.ToVec()*100);
        }
    }
}
