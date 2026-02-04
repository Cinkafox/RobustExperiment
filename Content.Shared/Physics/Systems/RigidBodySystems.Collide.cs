using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private readonly Dictionary<(Type, Type), ICollider> _colliderRegistry = new();
    
    public void InitializeColliders()
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
                Logger.Error($"Failed to initialize collider {type.Name}: {ex.Message}");
            }
        }
    }
    
    private void RegisterCollider(Type a, Type b, ICollider collider)
    {
        var key = (a, b);
        if (_colliderRegistry.ContainsKey(key))
        {
            Logger.Warning($"Duplicate collider registration for ({a.Name}, {b.Name})");
            return;
        }
        _colliderRegistry[key] = collider;
        Logger.Debug($"Registered collider for ({a.Name}, {b.Name})");
    }
    
    private void ResolveCollisions(float deltaTime)
    {
        var dynamicBodies = new List<(EntityUid, RigidBodyComponent, Transform3dComponent)>();
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            dynamicBodies.Add((uid, body, xform));
        }

        // O(n^2) broadphase (replace with spatial partitioning in production)
        for (var i = 0; i < dynamicBodies.Count; i++)
        {
            var (uidA, bodyA, xformA) = dynamicBodies[i];
            for (var j = i + 1; j < dynamicBodies.Count; j++)
            {
                var (uidB, bodyB, xformB) = dynamicBodies[j];
                SolveCollision(uidA, bodyA, xformA, uidB, bodyB, xformB, deltaTime);
            }
        }
    }
    
    private void SolveCollision(
        EntityUid uidA, RigidBodyComponent bodyA, Transform3dComponent xformA,
        EntityUid uidB, RigidBodyComponent bodyB, Transform3dComponent xformB,
        float deltaTime)
    {
        var shapeTypeA = bodyA.Shape.GetType();
        var shapeTypeB = bodyB.Shape.GetType();
        
        if (!_colliderRegistry.TryGetValue((shapeTypeA, shapeTypeB), out var collider))
            return;
        
        var contact = collider.ProcessCollision(
            new TransformedPhysicShape(xformA, bodyA.Shape),
            new TransformedPhysicShape(xformB, bodyB.Shape));

        if (!contact.HasContact) return;
        
        ResolveContact(uidA, bodyA, xformA, uidB, bodyB, xformB, contact, deltaTime);
    }
    
    private void ResolveContact(
        EntityUid uidA, RigidBodyComponent bodyA, Transform3dComponent xformA,
        EntityUid uidB, RigidBodyComponent bodyB, Transform3dComponent xformB,
        ManifoldPoints contact, float deltaTime)
    {
        if (bodyA.PhysType == PhysType.Static && bodyB.PhysType == PhysType.Static)
            return;
        
        var relativeVelocity = bodyB.LinearVelocity - bodyA.LinearVelocity;
        var velocityAlongNormal = Vector3.Dot(relativeVelocity, contact.Normal);
        
        if (velocityAlongNormal > 0f) return;
        
        const float restitution = 0.3f;
        var (staticFriction, dynamicFriction) = GetCombinedFriction(
            bodyA.SurfaceMaterial, bodyB.SurfaceMaterial);
        
        staticFriction = MathF.Min(staticFriction, bodyA.StaticFriction * bodyB.StaticFriction);
        dynamicFriction = MathF.Min(dynamicFriction, bodyA.DynamicFriction * bodyB.DynamicFriction);
        
        float invMassA = (bodyA.PhysType == PhysType.Dynamic) ? 1f / bodyA.Mass : 0f;
        float invMassB = (bodyB.PhysType == PhysType.Dynamic) ? 1f / bodyB.Mass : 0f;
        float impulseMagnitude = -(1f + restitution) * velocityAlongNormal;
        impulseMagnitude /= invMassA + invMassB;
        impulseMagnitude = MathF.Max(0f, impulseMagnitude);
        
        var impulse = contact.Normal * impulseMagnitude;
        
        if (bodyA.PhysType == PhysType.Dynamic)
            bodyA.LinearVelocity -= impulse * invMassA;
        
        if (bodyB.PhysType == PhysType.Dynamic)
            bodyB.LinearVelocity += impulse * invMassB;
        
        var tangent = relativeVelocity - contact.Normal * velocityAlongNormal;
        if (tangent.Length() > 0.001f)
        {
            tangent = Vector3.Normalize(tangent);
            var frictionImpulse = tangent * (-velocityAlongNormal * staticFriction);
            
            if (bodyA.PhysType == PhysType.Dynamic)
                bodyA.LinearVelocity -= frictionImpulse * invMassA;
            
            if (bodyB.PhysType == PhysType.Dynamic)
                bodyB.LinearVelocity += frictionImpulse * invMassB;
        }
        
        const float percent = 0.2f;
        const float slop = 0.01f;
        var correction = MathF.Max(contact.Depth - slop, 0f) / (invMassA + invMassB) * percent * contact.Normal;
        
        if (bodyA.PhysType == PhysType.Dynamic)
            xformA.LocalPosition -= correction * invMassA;
        
        if (bodyB.PhysType == PhysType.Dynamic)
            xformB.LocalPosition += correction * invMassB;

        // Logger.DebugS("physics", $"Collision: {ToPrettyString(uidA)} <-> {ToPrettyString(uidB)} | Depth: {contact.Depth:F3}");
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