﻿using System.Numerics;
using Content.Shared.StackSpriting;
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
    [Dependency] protected readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] protected readonly SharedEyeSystem _eyeSystem = default!;
    
    protected EntityQuery<InputMoverComponent> MoverQuery;
    public override void Initialize()
    { 
        MoverQuery = GetEntityQuery<InputMoverComponent>();
        InitializeInput();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<InputMoverComponent,EyeComponent>();
        while (query.MoveNext(out var uid, out var inputMoverComponent, out var eyeComponent))
        {
            var dir = inputMoverComponent.PushedButtons.ToDir();
            if (dir is Direction.Invalid)
            {
                PhysicsSystem.SetLinearVelocity(uid, Vector2.Zero);
                continue;
            }

            var angle = dir.ToAngle() + Angle.FromDegrees(270) - eyeComponent.Rotation;
            PhysicsSystem.SetLinearVelocity(uid, angle.ToVec()*20);
            //PhysicsSystem.SetAngularVelocity(uid, (float)(TransformSystem.GetWorldRotation(uid) - angle));
            //_transformSystem.SetWorldPosition(uid,_transformSystem.GetWorldPosition(uid) + angle.ToVec()*frameTime*4);
        }
    }
}
