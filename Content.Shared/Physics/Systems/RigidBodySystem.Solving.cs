using Content.Shared.Physics.Components;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private const float LinearVelocityEpsilonSq = 1e-4f;
    private const float AngularVelocityEpsilonSq = 1e-4f;
    private const float PositionEpsilonSq = 1e-6f;
    private const float RotationDotEpsilon = 3.8e-7f;

    private void ApplyGlobalForces(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var body))
        {
            if (body.PhysType == PhysType.Static || !ShouldPredict(uid)) continue;
            
            var gravityForce = new Vector3(0, -9.81f, 0) * body.Mass * deltaTime;
            ApplyForce(new Entity<RigidBodyComponent>(uid, body), gravityForce);
        }
    }
    
    private void IntegrateVelocities(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var body, out var transform))
        {
            if (body.PhysType == PhysType.Static || !ShouldPredict(uid)) continue;
        
            body.UpdateWorldInertia(transform.WorldRotation);
            
            var linearSpeed = body.LinearVelocity.Length();
            
            // ===== GROUNDED DAMPING =====
            // Apply stronger damping when grounded to prevent shaking
            if (body.IsGrounded)
            {
                // Strong damping on X/Z when grounded to prevent sliding jitter
                var groundDamping = MathF.Pow(0.1f, deltaTime); // Very strong damping
                body.LinearVelocity.X *= groundDamping;
                body.LinearVelocity.Z *= groundDamping;
                
                // Dampen small Y movements when grounded
                if (MathF.Abs(body.LinearVelocity.Y) < 0.5f)
                {
                    body.LinearVelocity.Y *= MathF.Pow(0.3f, deltaTime);
                }
                
                body.AngularVelocity *= MathF.Pow(0.85f, deltaTime);
            }
            else
            {
                // ===== LINEAR DAMPING (airborne) =====
                if (linearSpeed > 0.001f)
                {
                    const float linearDrag = 0.2f;
                    const float quadraticDrag = 0.02f;
                
                    var dragForce = body.LinearVelocity * linearDrag + 
                                    (body.LinearVelocity * linearSpeed) * quadraticDrag;
                
                    body.LinearVelocity -= dragForce * deltaTime;
                }
            }
        
            // ===== ANGULAR DAMPING =====
            if (!body.EnableAngularVelocity)
            {
                body.AngularVelocity = Vector3.Zero;
            }
            else
            {
                var angularSpeed = body.AngularVelocity.Length();

                if (angularSpeed > 0.01f)
                {
                    var angularDrag = body.IsGrounded ? 0.5f : 0.15f;
                    var dragTorque = body.AngularVelocity * angularDrag;

                    body.AngularVelocity -= dragTorque * deltaTime;
                }
            }
            
            const float maxAngularSpeed = 50f;

            var speed = body.AngularVelocity.Length();
            if (speed > maxAngularSpeed)
            {
                body.AngularVelocity =
                    Vector3.Normalize(body.AngularVelocity) *
                    maxAngularSpeed;
            }
        }
    }
    
    private void IntegratePositions(float deltaTime)
    {
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            if (body.PhysType == PhysType.Static || !ShouldPredict(uid)) continue;
            
            xform.SetLocalPositionNoDirty(xform.LocalPosition + body.LinearVelocity * deltaTime);
            
            if (!body.EnableAngularVelocity)
            {
                body.AngularVelocity = Vector3.Zero;
                continue;
            }
            
            var angVel = body.AngularVelocity;
            var angVelSq = angVel.LengthSquared();

            if (angVelSq > 1e-6f)
            {
                var angVelLen = MathF.Sqrt(angVelSq);
                var axis = angVel / angVelLen;
                var angle = angVelLen * deltaTime;
                
                var deltaRotation = Quaternion.CreateFromAxisAngle(axis, angle);
                xform.SetLocalRotationNoDirty(Quaternion.Normalize(
                    deltaRotation * xform.LocalRotation));
            }
        }
    }

    private void DirtyPhysics()
    {
        if (_netManager.IsClient && !_gameTiming.InPrediction)
            return;

        var query = AllEntityQuery<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var body))
        {
            if (!ShouldPredict(uid) || !TryComp<Transform3dComponent>(uid, out var xform))
                continue;

            if (!body.HasNetSnapshot)
            {
                CaptureNetSnapshot(body, xform);
                continue;
            }

            if (Vector3.DistanceSquared(body.LinearVelocity, body.LastNetLinearVelocity) > LinearVelocityEpsilonSq)
            {
                DirtyField(uid, body, nameof(RigidBodyComponent.LinearVelocity));
                body.LastNetLinearVelocity = body.LinearVelocity;
            }

            if (Vector3.DistanceSquared(body.AngularVelocity, body.LastNetAngularVelocity) > AngularVelocityEpsilonSq)
            {
                DirtyField(uid, body, nameof(RigidBodyComponent.AngularVelocity));
                body.LastNetAngularVelocity = body.AngularVelocity;
            }

            if (Vector3.DistanceSquared(xform.LocalPosition, body.LastNetPosition) > PositionEpsilonSq)
            {
                DirtyField(uid, xform, nameof(Transform3dComponent.LocalPosition));
                body.LastNetPosition = xform.LocalPosition;
            }

            if (1f - MathF.Abs(Quaternion.Dot(xform.LocalRotation, body.LastNetRotation)) > RotationDotEpsilon)
            {
                DirtyField(uid, xform, nameof(Transform3dComponent.LocalRotation));
                body.LastNetRotation = xform.LocalRotation;
            }
        }
    }
    
    private void UpdateGroundStates(float deltaTime)
    {
        const float UP_THRESHOLD = 0.7f;
        const float VELOCITY_THRESHOLD = 0.5f;
        
        var query = AllEntityQuery<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var body))
        {
            if (!ShouldPredict(uid))
                continue;

            body.ResetGroundState();
        }
        
        foreach (var manifold in _contacts)
        {
            var contact = manifold.Points;
            
            var bodyA = manifold.BodyA.Comp;
            var bodyB = manifold.BodyB.Comp;
        
            var normalDotUpA = Vector3.Dot(contact.Normal, Vector3.UnitY);
            var normalDotUpB = Vector3.Dot(contact.Normal, Vector3.UnitY);
            
            if (bodyA.PhysType == PhysType.Dynamic &&
                ShouldPredict(manifold.BodyA.Owner) &&
                normalDotUpA > UP_THRESHOLD)
            {
                var relVel = Vector3.Dot(bodyA.LinearVelocity, contact.Normal);
                if (relVel <= VELOCITY_THRESHOLD)
                    bodyA.AddGroundContact(normalDotUpA);
            }
        
            if (bodyB.PhysType == PhysType.Dynamic &&
                ShouldPredict(manifold.BodyB.Owner) &&
                normalDotUpB > UP_THRESHOLD)
            {
                var relVel = Vector3.Dot(bodyB.LinearVelocity, -contact.Normal);
                if (relVel <= VELOCITY_THRESHOLD)
                    bodyB.AddGroundContact(normalDotUpB);
            }
        }
    }

    private void SimulateStep(float deltaTime, int steps = 1)
    {
        var stepDt = deltaTime / steps;
        for (var i = 0; i < steps; i++)
        {
            ApplyGlobalForces(stepDt);
            IntegrateVelocities(stepDt);
            PrepareConstraints(stepDt);
            ResolveCollisions(stepDt);
            SolveConstraints(stepDt);
            UpdateGroundStates(stepDt);
            IntegratePositions(stepDt);
        }
        
        DirtyPhysics();
    }
}