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
        var gravityDelta = 9.81f * deltaTime;

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];
            if (!entry.Dynamic || !entry.Predicted)
                continue;

            entry.Body.LinearVelocity.Y -= gravityDelta;
        }
    }

    private void IntegrateVelocities(float deltaTime)
    {
        var groundDamping = MathF.Pow(0.1f, deltaTime);
        var groundYDamping = MathF.Pow(0.3f, deltaTime);
        var groundAngularDamping = MathF.Pow(0.85f, deltaTime);

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];
            if (!entry.Dynamic || !entry.Predicted)
                continue;

            var body = entry.Body;
            var transform = entry.Xform;

            if (entry.HasConstraints)
                body.UpdateWorldInertia(transform.WorldRotation);

            // ===== GROUNDED DAMPING =====
            // Apply stronger damping when grounded to prevent shaking
            if (body.IsGrounded)
            {
                // Strong damping on X/Z when grounded to prevent sliding jitter
                body.LinearVelocity.X *= groundDamping;
                body.LinearVelocity.Z *= groundDamping;

                // Dampen small Y movements when grounded
                if (MathF.Abs(body.LinearVelocity.Y) < 0.5f)
                {
                    body.LinearVelocity.Y *= groundYDamping;
                }

                body.AngularVelocity *= groundAngularDamping;
            }
            else
            {
                // ===== LINEAR DAMPING (airborne) =====
                var linearSpeed = body.LinearVelocity.Length();

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
                body.AngularVelocity *= maxAngularSpeed / speed;
        }
    }

    private void IntegratePositions(float deltaTime)
    {
        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];
            if (!entry.Dynamic || !entry.Predicted)
                continue;

            var body = entry.Body;
            var xform = entry.Xform;

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

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];
            if (!entry.Predicted)
                continue;

            var uid = entry.Uid;
            var body = entry.Body;
            var xform = entry.Xform;

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

    private void UpdateGroundStates()
    {
        const float upThreshold = 0.7f;
        const float velocityThreshold = 0.5f;

        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];
            if (entry.Predicted)
                entry.Body.ResetGroundState();
        }

        foreach (var manifold in _contacts)
        {
            var contact = manifold.Points;

            var bodyA = manifold.BodyA.Comp;
            var bodyB = manifold.BodyB.Comp;

            var normalDotUp = Vector3.Dot(contact.Normal, Vector3.UnitY);

            if (bodyA.PhysType == PhysType.Dynamic &&
                manifold.PredictedA &&
                normalDotUp > upThreshold)
            {
                var relVel = Vector3.Dot(bodyA.LinearVelocity, contact.Normal);
                if (relVel <= velocityThreshold)
                    bodyA.AddGroundContact(normalDotUp);
            }

            if (bodyB.PhysType == PhysType.Dynamic &&
                manifold.PredictedB &&
                normalDotUp > upThreshold)
            {
                var relVel = Vector3.Dot(bodyB.LinearVelocity, -contact.Normal);
                if (relVel <= velocityThreshold)
                    bodyB.AddGroundContact(normalDotUp);
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
            ResolveCollisions();
            SolveConstraints(stepDt);
            UpdateGroundStates();
            IntegratePositions(stepDt);
        }

        DirtyPhysics();
    }
}