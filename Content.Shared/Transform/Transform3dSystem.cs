using Content.Shared.Utils;
using Robust.Shared.Map;

namespace Content.Shared.Transform;

public sealed class Transform3dSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    
    public override void Initialize()
    {
        EntityManager.ComponentAdded += EntityManagerOnComponentAdded;
        
        SubscribeLocalEvent<Transform3dComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<Transform3dComponent> ent, ref ComponentInit args)
    {
        
    }

    public void AttachTo(Entity<Transform3dComponent?> entity, Entity<Transform3dComponent?> parent)
    {
        if(!Resolve(entity, ref entity.Comp) || !Resolve(parent, ref parent.Comp))
            return;
        
        parent.Comp.AddChild(entity!);
    }
    
    public void AttachTo(Entity<Transform3dComponent?> entity, MapId mapId)
    {
        if(!Resolve(entity, ref entity.Comp) || !_mapSystem.TryGetMap(mapId, out var mapEnt))
            return;
        
        Comp<Transform3dComponent>(mapEnt.Value).AddChild(entity!);
    }

    public void SetPosition(Entity<Transform3dComponent?> entity, Vector3 pos)
    {
        if(!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.LocalPosition = pos;
    }
    
    public void SetRotation(Entity<Transform3dComponent?> entity, Angle3d rotation)
    {
        if(!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.LocalRotation = rotation;
    }

    public override void Shutdown()
    {
        EntityManager.ComponentAdded -= EntityManagerOnComponentAdded;
    }

    private void EntityManagerOnComponentAdded(AddedComponentEventArgs obj)
    {
        if (obj.BaseArgs.Component is TransformComponent)
        {
            AddComp<Transform3dComponent>(obj.BaseArgs.Owner);
        }
    }
}
