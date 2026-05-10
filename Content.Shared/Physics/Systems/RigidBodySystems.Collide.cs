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
        var activeKeys = new HashSet<ContactKey>();
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
                var contact = SolveCollision(uidA, bodyA, xformA, uidB, bodyB, xformB, deltaTime);
                
                if (contact is null)
                    continue;

                var evA = new CollideObjectEvent(contact.BodyB);
                var evB = new CollideObjectEvent(contact.BodyA);
                
                RaiseLocalEvent(uidA, evA);
                RaiseLocalEvent(uidB, evB);
                
                if(evA.Cancelled || evB.Cancelled) 
                    continue;
                
                ResolveContact(uidA, bodyA, xformA, uidB, bodyB, xformB, contact.Points, deltaTime);
                
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
        ManifoldPoints contact, float deltaTime)
    {
        if (bodyA.PhysType == PhysType.Static && bodyB.PhysType == PhysType.Static)
            return;
        
        var ra = contact.A - xformA.LocalPosition;
        var rb = contact.B - xformB.LocalPosition;
        
        
        var restitution = float.Max(bodyA.Restitution, bodyB.Restitution);
        var friction = float.Max(bodyA.Friction, bodyB.Friction);
        
        var warmImpulse = contact.Normal * contact.NormalImpulse;

        if (bodyA.PhysType == PhysType.Dynamic)
        {
            bodyA.LinearVelocity -= warmImpulse * bodyA.InvMass;

            if (bodyA.EnableAngularVelocity)
            {
                bodyA.AngularVelocity -=
                    Vector3.Cross(ra, warmImpulse) *
                    bodyA.InvInertia;
            }
        }

        if (bodyB.PhysType == PhysType.Dynamic)
        {
            bodyB.LinearVelocity += warmImpulse * bodyB.InvMass;

            if (bodyB.EnableAngularVelocity)
            {
                bodyB.AngularVelocity +=
                    Vector3.Cross(rb, warmImpulse) *
                    bodyB.InvInertia;
            }
        }
        
        var warmTangent = contact.TangentImpulse;

        if (bodyA.PhysType == PhysType.Dynamic)
        {
            bodyA.LinearVelocity -= warmTangent * bodyA.InvMass;
        }

        if (bodyB.PhysType == PhysType.Dynamic)
        {
            bodyB.LinearVelocity += warmTangent * bodyB.InvMass;
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
            Vector3.Dot(raCrossN, raCrossN) * bodyA.InvInertia +
            Vector3.Dot(rbCrossN, rbCrossN) * bodyB.InvInertia;

        var impulseDenom =
            bodyA.InvMass +
            bodyB.InvMass +
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
                bodyA.InvMass +
                bodyB.InvMass +
                Vector3.Dot(raCrossT, raCrossT) * bodyA.InvInertia +
                Vector3.Dot(rbCrossT, rbCrossT) * bodyB.InvInertia;

            
            var jt = -Vector3.Dot(relativeVelocity, tangent);
            
            var frictionImpulse = jt / frictionDenom;
            
            var maxFriction = friction * impulseMagnitude;
            frictionImpulse = Math.Clamp(frictionImpulse, -maxFriction, maxFriction);
            
            var frictionVector = tangent * frictionImpulse;
            
            if (bodyA.PhysType == PhysType.Dynamic)
                bodyA.LinearVelocity -= frictionVector * bodyA.InvMass;
            
            if (bodyB.PhysType == PhysType.Dynamic)
                bodyB.LinearVelocity += frictionVector * bodyB.InvMass;
            
            contact.TangentImpulse += frictionVector;
            contact.TangentImpulse = Vector3.Clamp(
                contact.TangentImpulse,
                new Vector3(-maxFriction),
                new Vector3(maxFriction));
        }
        
        contact.NormalImpulse += impulseMagnitude;
        contact.NormalImpulse =
            MathF.Max(0f, contact.NormalImpulse);
        
        if (bodyA.PhysType == PhysType.Dynamic)
        {
            bodyA.LinearVelocity -= impulse * bodyA.InvMass;

            if (bodyA.EnableAngularVelocity)
            {
                bodyA.AngularVelocity -=
                    Vector3.Cross(ra, impulse) *
                    bodyA.InvInertia;
            }
        }

        if (bodyB.PhysType == PhysType.Dynamic)
        {
            bodyB.LinearVelocity += impulse * bodyB.InvMass;

            if (bodyB.EnableAngularVelocity)
            {
                bodyB.AngularVelocity +=
                    Vector3.Cross(rb, impulse) *
                    bodyB.InvInertia;
            }
        }
        
        var correctionPercent = 0.05f;
        const float slop = 0.03f;
        
        // Reduce correction when bodies are grounded to prevent shaking
        if (bodyA.IsGrounded || bodyB.IsGrounded)
        {
            correctionPercent = 0.05f; // Much gentler correction when grounded
        }
        
        var invMassSum = bodyA.InvMass + bodyB.InvMass;
        
        var correction = MathF.Max(contact.Depth - slop, 0f) / invMassSum * correctionPercent * contact.Normal;
        
        if (bodyA.PhysType == PhysType.Dynamic)
            xformA.LocalPosition -= correction * bodyA.InvMass;

        if (bodyB.PhysType == PhysType.Dynamic)
            xformB.LocalPosition += correction * bodyB.InvMass;
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