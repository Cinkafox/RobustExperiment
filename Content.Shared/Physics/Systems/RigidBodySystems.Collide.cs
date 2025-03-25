using System.Linq;
using System.Reflection;
using Content.Shared.Physics.Colliders;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private Dictionary<Type, object> _instances = new();
    private Dictionary<(Type, Type), object> _colliders = new();
    
    public void InitializeColliders()
    {
        var types = _reflectionManager
            .FindTypesWithAttribute<ColliderRegisterAttribute>();
        
        foreach (var collider in types)
        {
            collider.TryGetCustomAttribute(typeof(ColliderRegisterAttribute), out var ratattr);
            var attr = (ColliderRegisterAttribute)ratattr!;
            
            if (!_instances.TryGetValue(collider, out var instance))
            {
                instance = _sandboxHelper.CreateInstance(collider);
                _instances.Add(collider,instance);
            }
            
            _colliders.Add((attr.A, attr.B), instance);
            if(attr.A != attr.B)
                _colliders.Add((attr.B, attr.A), new RevCollider(instance));
        }
    }
    
    
    private void SolveCollide(Entity<RigidBodyComponent> a, Entity<RigidBodyComponent> b)
    {
        var aTransform = Comp<Transform3dComponent>(a);
        var bTransform = Comp<Transform3dComponent>(b);
        
        var aShape = a.Comp.Shape;
        var bShape = b.Comp.Shape;
        

        var obj = (ICollider) _colliders[(aShape.GetType(), bShape.GetType())];

        var contact = obj.ProcessCollision(new TransformedPhysicShape(aTransform, aShape),
            new TransformedPhysicShape(bTransform, bShape));
        
        if(!contact.HasContact) return;
        
        ProcessPhysType(new Entity<RigidBodyComponent, Transform3dComponent>(a.Owner,a.Comp, aTransform), 
            contact, b.Comp.PhysType == PhysType.Kinematic);
        //ProcessPhysType(new Entity<RigidBodyComponent, Transform3dComponent>(b.Owner,b.Comp, bTransform), 
            //contact, a.Comp.PhysType == PhysType.Kinematic);
        
        
        Logger.Debug(contact.Normal + " " + contact.Depth + " " + contact.A + " " + contact.B + " " + aTransform.LocalPosition);
    }

    private void ProcessPhysType(Entity<RigidBodyComponent, Transform3dComponent> physicEntity, ManifoldPoints contact, bool anotherProcess)
    {
        if (physicEntity.Comp1.PhysType != PhysType.Kinematic) 
            return;

        ApplyForce(physicEntity, -(physicEntity.Comp1.LinearVelocity - physicEntity.Comp1.LinearVelocity * contact.Normal) * physicEntity.Comp1.Mass);
    }
}