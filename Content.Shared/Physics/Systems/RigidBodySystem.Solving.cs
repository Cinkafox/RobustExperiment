using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private void ApplyGlobalForces(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var body))
        {
            if (body.PhysType == PhysType.Static) continue;
            
            var gravityForce = new Vector3(0, -9.81f, 0) * body.Mass * deltaTime;
            ApplyForce(new Entity<RigidBodyComponent>(uid, body), gravityForce);
        }
    }
    
    private void IntegrateVelocities(float deltaTime)
    {
        var query = AllEntityQuery<RigidBodyComponent>();
        while (query.MoveNext(out var uid, out var body))
        {
            if (body.PhysType == PhysType.Static) continue;
        
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
            var angularSpeed = body.AngularVelocity.Length();
            if (angularSpeed > 0.01f)
            {
                // Stronger angular damping when grounded
                var angularDrag = body.IsGrounded ? 0.5f : 0.15f;
                var dragTorque = body.AngularVelocity * angularDrag;
                body.AngularVelocity -= dragTorque * deltaTime;
            }
        }
    }
    
    private void IntegratePositions(float deltaTime)
    {
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            if (body.PhysType == PhysType.Static) continue;
            
            xform.LocalPosition += body.LinearVelocity * deltaTime;
            
            if (body.AngularVelocity.Length() > 0.001f)
            {
                var rotationAxis = Vector3.Normalize(body.AngularVelocity);
                var rotationAngle = body.AngularVelocity.Length() * deltaTime;
                var deltaRotation = Quaternion.CreateFromAxisAngle(rotationAxis, rotationAngle);
                xform.LocalRotation = deltaRotation * xform.LocalRotation;
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
            body.ResetGroundState();
        }
        
        foreach (var manifold in _contacts)
        {
            var contact = manifold.Points;
            
            var bodyA = manifold.BodyA.Comp;
            var bodyB = manifold.BodyB.Comp;
        
            var normalDotUpA = Vector3.Dot(contact.Normal, Vector3.UnitY);
            var normalDotUpB = Vector3.Dot(contact.Normal, Vector3.UnitY);
            
            if (bodyA.PhysType == PhysType.Dynamic && normalDotUpA > UP_THRESHOLD)
            {
                var relVel = Vector3.Dot(bodyA.LinearVelocity, contact.Normal);
                if (relVel <= VELOCITY_THRESHOLD)
                    bodyA.AddGroundContact(normalDotUpA);
            }
        
            if (bodyB.PhysType == PhysType.Dynamic && normalDotUpB > UP_THRESHOLD)
            {
                var relVel = Vector3.Dot(bodyB.LinearVelocity, -contact.Normal);
                if (relVel <= VELOCITY_THRESHOLD)
                    bodyB.AddGroundContact(normalDotUpB);
            }
        }
    }

    private void SimulateStep(float deltaTime, int perf = 1)
    {
        for (int i = 0; i < perf; i++)
        {
            ApplyGlobalForces(deltaTime / perf);
            IntegrateVelocities(deltaTime / perf);
            ResolveCollisions(deltaTime / perf);
            UpdateGroundStates(deltaTime / perf);
            IntegratePositions(deltaTime / perf);
        }
    }
}