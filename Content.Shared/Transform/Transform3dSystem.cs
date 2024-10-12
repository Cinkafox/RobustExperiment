namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem : EntitySystem
{
    private EntityQuery<Transform3dComponent> _xformQuery;
    
    public override void Initialize()
    {
        _xformQuery = GetEntityQuery<Transform3dComponent>();
        EntityManager.ComponentAdded += EntityManagerOnComponentAdded;
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
