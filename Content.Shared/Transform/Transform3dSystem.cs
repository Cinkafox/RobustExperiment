namespace Content.Shared.Transform;

public sealed partial class Transform3dSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public EntityQuery<Transform3dComponent> XformQuery;
    
    public override void Initialize()
    {
        XformQuery = GetEntityQuery<Transform3dComponent>();
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
