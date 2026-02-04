using Content.Shared.Physics.Components;
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
        
            // ===== LINEAR DAMPING =====
            // Air/fluid resistance (quadratic drag at high speeds, linear at low speeds)
            var linearSpeed = body.LinearVelocity.Length();
            if (linearSpeed > 0.01f)
            {
                const float linearDrag = 0.05f;   // Low-speed damping
                const float quadraticDrag = 0.002f; // High-speed damping
            
                var dragForce = body.LinearVelocity * linearDrag + 
                                (body.LinearVelocity * linearSpeed) * quadraticDrag;
            
                body.LinearVelocity -= dragForce * deltaTime;
            }
        
            // ===== ANGULAR DAMPING =====
            // Rotational resistance (typically stronger than linear damping)
            var angularSpeed = body.AngularVelocity.Length();
            if (angularSpeed > 0.01f)
            {
                const float angularDrag = 0.15f; // Higher damping for rotation
            
                var dragTorque = body.AngularVelocity * angularDrag;
                body.AngularVelocity -= dragTorque * deltaTime;
            }
        
            // ===== SURFACE FRICTION WHEN NOT COLLIDING =====
            // Simulate ground friction when object is near rest on a surface
            // (Requires raycast to detect ground - simplified here)
            if (linearSpeed < 0.1f && MathF.Abs(body.LinearVelocity.Y) < 0.01f)
            {
                // Apply extra damping to X/Z when near ground
                body.LinearVelocity.X *= MathF.Pow(0.85f, deltaTime);
                body.LinearVelocity.Z *= MathF.Pow(0.85f, deltaTime);
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

    private void SimulateStep(float deltaTime)
    {
        ApplyGlobalForces(deltaTime);
        IntegrateVelocities(deltaTime);
        ResolveCollisions(deltaTime);
        IntegratePositions(deltaTime);
    }
}