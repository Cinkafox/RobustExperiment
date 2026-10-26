using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private readonly Dictionary<(Type, Type), ICollider> _colliderRegistry = new();
    private readonly List<(EntityUid, RigidBodyComponent, Transform3dComponent)> _dynamicBodies = new();
    
    private readonly List<ContactManifold> _contacts = new();

    private void InitializeColliders()
    {
        var colliderTypes = _reflectionManager
            .FindTypesWithAttribute<ColliderRegisterAttribute>();

        foreach (var type in colliderTypes)
        {
            if(!type.TryGetCustomAttribute<ColliderRegisterAttribute>(out var attr)) 
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
    
    private void ResolveCollisions(float deltaTime)
    {
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        _dynamicBodies.Clear();
        _contacts.Clear();
        
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            _dynamicBodies.Add((uid, body, xform));
        }

        // O(n^2) broadphase (replace with spatial partitioning in production)
        for (var i = 0; i < _dynamicBodies.Count; i++)
        {
            var (uidA, bodyA, xformA) = _dynamicBodies[i];
            for (var j = i + 1; j < _dynamicBodies.Count; j++)
            {
                var (uidB, bodyB, xformB) = _dynamicBodies[j];

                var predictA = ShouldPredict(uidA);
                var predictB = ShouldPredict(uidB);

                if (!predictA && !predictB)
                    continue;

                var contact = SolveCollision(uidA, bodyA, xformA, uidB, bodyB, xformB, deltaTime);
                
                if (contact is null)
                    continue;

                CollideObjectEvent? evA = null;
                CollideObjectEvent? evB = null;

                if (predictA)
                {
                    evA = new CollideObjectEvent(contact.BodyB);
                    RaiseLocalEvent(uidA, evA);
                }

                if (predictB)
                {
                    evB = new CollideObjectEvent(contact.BodyA);
                    RaiseLocalEvent(uidB, evB);
                }
                
                if(evA?.Cancelled == true || evB?.Cancelled == true) 
                    continue;
                
                ResolveContact(uidA, bodyA, xformA, uidB, bodyB, xformB, contact.Points, deltaTime, predictA, predictB);
                
                _contacts.Add(contact);
            }
        }
    }
    
    private ContactManifold? SolveCollision(
        EntityUid uidA, RigidBodyComponent bodyA, Transform3dComponent xformA,
        EntityUid uidB, RigidBodyComponent bodyB, Transform3dComponent xformB,
        float deltaTime)
    {
        var shapeTypeA = bodyA.Shape.GetType();
        var shapeTypeB = bodyB.Shape.GetType();
        
        if (!_colliderRegistry.TryGetValue((shapeTypeA, shapeTypeB), out var collider))
            throw new Exception($"Collision could not be found: {shapeTypeA}, {shapeTypeB}");
        
        var contact = collider.ProcessCollision(
            new TransformedPhysicShape(xformA, bodyA.Shape),
            new TransformedPhysicShape(xformB, bodyB.Shape));
        
        if (!contact.HasContact)
            return null;
        
        return new ContactManifold(contact, 
            new Entity<RigidBodyComponent>(uidA, bodyA), 
            new Entity<RigidBodyComponent>(uidB, bodyB));
    }
    
    private void ResolveContact(
        EntityUid uidA, RigidBodyComponent bodyA, Transform3dComponent xformA,
        EntityUid uidB, RigidBodyComponent bodyB, Transform3dComponent xformB,
        ManifoldPoints contact, float deltaTime, bool predictA, bool predictB)
    {
        var dynamicA = predictA && bodyA.PhysType == PhysType.Dynamic;
        var dynamicB = predictB && bodyB.PhysType == PhysType.Dynamic;

        if (!dynamicA && !dynamicB)
            return;

        var invMassA = dynamicA ? bodyA.InvMass : 0f;
        var invMassB = dynamicB ? bodyB.InvMass : 0f;
        var invInertiaA = dynamicA ? bodyA.InvInertia : 0f;
        var invInertiaB = dynamicB ? bodyB.InvInertia : 0f;

        var ra = contact.A - xformA.LocalPosition;
        var rb = contact.B - xformB.LocalPosition;
        
        
        var restitution = float.Max(bodyA.Restitution, bodyB.Restitution);
        var friction = float.Max(bodyA.Friction, bodyB.Friction);
        
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
        
        var correctionPercent = 0.05f;
        const float slop = 0.03f;
        
        // Reduce correction when bodies are grounded to prevent shaking
        if (bodyA.IsGrounded || bodyB.IsGrounded)
        {
            correctionPercent = 0.05f; // Much gentler correction when grounded
        }
        
        var invMassSum = invMassA + invMassB;
        
        var correction = MathF.Max(contact.Depth - slop, 0f) / invMassSum * correctionPercent * contact.Normal;
        
        if (dynamicA)
            xformA.SetLocalPositionNoDirty(xformA.LocalPosition - correction * invMassA);

        if (dynamicB)
            xformB.SetLocalPositionNoDirty(xformB.LocalPosition + correction * invMassB);
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