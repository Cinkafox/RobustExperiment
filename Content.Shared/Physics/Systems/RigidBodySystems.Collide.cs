using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private readonly Dictionary<(Type, Type), ICollider> _colliderRegistry = new();

    private readonly List<ContactManifold> _contacts = new();

    private void InitializeColliders()
    {
        var colliderTypes = _reflectionManager
            .FindTypesWithAttribute<ColliderRegisterAttribute>();

        foreach (var type in colliderTypes)
        {
            if (!type.TryGetCustomAttribute<ColliderRegisterAttribute>(out var attr))
                continue;

            try
            {
                var instance = (ICollider)_sandboxHelper.CreateInstance(type);
                RegisterCollider(attr.A, attr.B, instance);
                if (attr.A != attr.B)
                    RegisterCollider(attr.B, attr.A, new RevCollider(instance));
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize collider {type.Name}: {ex.Message}");
            }
        }
    }

    private void RegisterCollider(Type a, Type b, ICollider collider)
    {
        var key = (a, b);
        if (_colliderRegistry.ContainsKey(key))
        {
            Log.Warning($"Duplicate collider registration for ({a.Name}, {b.Name})");
            return;
        }

        _colliderRegistry[key] = collider;
        Log.Debug($"Registered collider for ({a.Name}, {b.Name})");
    }

    private void ResolveCollisions()
    {
        UpdateBroadphase();
        _contacts.Clear();

        // Sweep-and-prune broadphase over cached body bounds.
        for (var i = 0; i < _bodyCount; i++)
        {
            var sweepA = _sweepEntries[i];
            ref var a = ref _bodyCache[sweepA.BodyIndex];

            for (var j = i + 1; j < _bodyCount; j++)
            {
                var sweepB = _sweepEntries[j];

                if (sweepB.MinX > a.AabbMax.X)
                    break;

                ref var b = ref _bodyCache[sweepB.BodyIndex];

                if ((!a.Dynamic && !b.Dynamic) || (!a.Predicted && !b.Predicted))
                    continue;

                if (!AabbOverlaps(in a, in b))
                    continue;

                var contact = SolveCollision(in a, in b);

                if (contact is not { } manifold)
                    continue;

                CollideObjectEvent? evA = null;
                CollideObjectEvent? evB = null;

                if (a.Predicted)
                {
                    evA = new CollideObjectEvent(manifold.BodyB);
                    RaiseLocalEvent(a.Uid, evA);
                }

                if (b.Predicted)
                {
                    evB = new CollideObjectEvent(manifold.BodyA);
                    RaiseLocalEvent(b.Uid, evB);
                }

                if (evA?.Cancelled == true || evB?.Cancelled == true)
                    continue;

                ResolveContact(ref a, ref b, ref manifold.Points);

                _contacts.Add(manifold);
            }
        }
    }

    private ContactManifold? SolveCollision(in BodyCacheEntry a, in BodyCacheEntry b)
    {
        var bodyA = a.Body;
        var bodyB = b.Body;
        var shapeTypeA = a.ShapeType;
        var shapeTypeB = b.ShapeType;

        if (!_colliderRegistry.TryGetValue((shapeTypeA, shapeTypeB), out var collider))
            throw new Exception($"Collision could not be found: {shapeTypeA}, {shapeTypeB}");

        var contact = collider.ProcessCollision(
            new TransformedPhysicShape(bodyA.Shape, a.WorldPosition, a.WorldRotation, a.WorldScale),
            new TransformedPhysicShape(bodyB.Shape, b.WorldPosition, b.WorldRotation, b.WorldScale));

        if (!contact.HasContact)
            return null;

        return new ContactManifold(contact,
            new Entity<RigidBodyComponent>(a.Uid, bodyA),
            new Entity<RigidBodyComponent>(b.Uid, bodyB),
            a.Predicted,
            b.Predicted);
    }

    private static void ResolveContact(
        ref BodyCacheEntry a,
        ref BodyCacheEntry b,
        ref ManifoldPoints contact)
    {
        var bodyA = a.Body;
        var bodyB = b.Body;
        var xformA = a.Xform;
        var xformB = b.Xform;

        var dynamicA = a.Predicted && a.Dynamic;
        var dynamicB = b.Predicted && b.Dynamic;

        if (!dynamicA && !dynamicB)
            return;

        var invMassA = dynamicA ? a.InvMass : 0f;
        var invMassB = dynamicB ? b.InvMass : 0f;
        var invInertiaA = dynamicA ? a.InvInertia : 0f;
        var invInertiaB = dynamicB ? b.InvInertia : 0f;

        var ra = contact.A - xformA.LocalPosition;
        var rb = contact.B - xformB.LocalPosition;


        var restitution = float.Max(a.Restitution, b.Restitution);
        var friction = float.Max(a.Friction, b.Friction);

        var warmImpulse = contact.Normal * contact.NormalImpulse;

        if (dynamicA)
        {
            bodyA.LinearVelocity -= warmImpulse * invMassA;

            if (bodyA.EnableAngularVelocity)
            {
                bodyA.AngularVelocity -=
                    Vector3.Cross(ra, warmImpulse) *
                    invInertiaA;
            }
        }

        if (dynamicB)
        {
            bodyB.LinearVelocity += warmImpulse * invMassB;

            if (bodyB.EnableAngularVelocity)
            {
                bodyB.AngularVelocity +=
                    Vector3.Cross(rb, warmImpulse) *
                    invInertiaB;
            }
        }

        var warmTangent = contact.TangentImpulse;

        if (dynamicA)
        {
            bodyA.LinearVelocity -= warmTangent * invMassA;
        }

        if (dynamicB)
        {
            bodyB.LinearVelocity += warmTangent * invMassB;
        }

        var velA =
            bodyA.LinearVelocity +
            Vector3.Cross(bodyA.AngularVelocity, ra);

        var velB =
            bodyB.LinearVelocity +
            Vector3.Cross(bodyB.AngularVelocity, rb);

        var relativeVelocity = velB - velA;

        var velocityAlongNormal = Vector3.Dot(relativeVelocity, contact.Normal);

        if (velocityAlongNormal > 0f) return;

        var raCrossN = Vector3.Cross(ra, contact.Normal);
        var rbCrossN = Vector3.Cross(rb, contact.Normal);

        var angularFactor =
            Vector3.Dot(raCrossN, raCrossN) * invInertiaA +
            Vector3.Dot(rbCrossN, rbCrossN) * invInertiaB;

        var impulseDenom =
            invMassA +
            invMassB +
            angularFactor;

        const float maxImpulse = 50f;

        var impulseMagnitude = -(1f + restitution) * velocityAlongNormal;
        impulseMagnitude /= impulseDenom;
        impulseMagnitude = Math.Clamp(impulseMagnitude, 0f, maxImpulse);

        var impulse = contact.Normal * impulseMagnitude;

        var tangent = relativeVelocity - contact.Normal * velocityAlongNormal;

        if (tangent.LengthSquared() > 1e-6f)
        {
            tangent = Vector3.Normalize(tangent);

            var raCrossT = Vector3.Cross(ra, tangent);
            var rbCrossT = Vector3.Cross(rb, tangent);

            var frictionDenom =
                invMassA +
                invMassB +
                Vector3.Dot(raCrossT, raCrossT) * invInertiaA +
                Vector3.Dot(rbCrossT, rbCrossT) * invInertiaB;


            var jt = -Vector3.Dot(relativeVelocity, tangent);

            var frictionImpulse = jt / frictionDenom;

            var maxFriction = friction * impulseMagnitude;
            frictionImpulse = Math.Clamp(frictionImpulse, -maxFriction, maxFriction);

            var frictionVector = tangent * frictionImpulse;

            if (dynamicA)
                bodyA.LinearVelocity -= frictionVector * invMassA;

            if (dynamicB)
                bodyB.LinearVelocity += frictionVector * invMassB;

            contact.TangentImpulse += frictionVector;
            contact.TangentImpulse = Vector3.Clamp(
                contact.TangentImpulse,
                new Vector3(-maxFriction),
                new Vector3(maxFriction));
        }

        contact.NormalImpulse += impulseMagnitude;
        contact.NormalImpulse =
            MathF.Max(0f, contact.NormalImpulse);

        if (dynamicA)
        {
            bodyA.LinearVelocity -= impulse * invMassA;

            if (bodyA.EnableAngularVelocity)
            {
                bodyA.AngularVelocity -=
                    Vector3.Cross(ra, impulse) *
                    invInertiaA;
            }
        }

        if (dynamicB)
        {
            bodyB.LinearVelocity += impulse * invMassB;

            if (bodyB.EnableAngularVelocity)
            {
                bodyB.AngularVelocity +=
                    Vector3.Cross(rb, impulse) *
                    invInertiaB;
            }
        }

        const float correctionPercent = 0.05f;
        const float slop = 0.03f;

        var invMassSum = invMassA + invMassB;

        var correction = MathF.Max(contact.Depth - slop, 0f) / invMassSum * correctionPercent * contact.Normal;

        if (dynamicA)
        {
            xformA.SetLocalPositionNoDirty(xformA.LocalPosition - correction * invMassA);
            UpdateBodyPosition(ref a);
        }

        if (dynamicB)
        {
            xformB.SetLocalPositionNoDirty(xformB.LocalPosition + correction * invMassB);
            UpdateBodyPosition(ref b);
        }
    }
}

internal sealed class RevCollider : ICollider
{
    private readonly ICollider _inner;

    public RevCollider(ICollider inner) => _inner = inner;

    public ManifoldPoints ProcessCollision(TransformedPhysicShape a, TransformedPhysicShape b)
    {
        var result = _inner.ProcessCollision(b, a);
        result.Normal = -result.Normal;
        return result;
    }
}