using Content.Shared.Bone;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;

namespace Content.Shared.Items;

public sealed class ItemsSystem : EntitySystem
{
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3dSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<CollectibleComponent, CollideObjectEvent>(OnCollide);
    }

    private void OnCollide(Entity<CollectibleComponent> ent, ref CollideObjectEvent args)
    {
        if(ent.Comp.IsTaken ||
           !TryComp<ItemCollectorComponent>(args.CollidedEntity, out var collided) || 
           !collided.IsEmpty || 
           !_boneSystem.TryGetBone(args.CollidedEntity.Owner, collided.BoneName, out var bone))
            return;
        
        args.Cancel();
        
        ent.Comp.TakenBy = ent;
        
        _transform3dSystem.SetParent(ent, bone);
        var transform = Comp<Transform3dComponent>(ent);
        transform.LocalPosition = ent.Comp.Position;
        transform.LocalAngle = ent.Comp.Rotation;
        transform.LocalScale = ent.Comp.Scale;

        RemComp<RigidBodyComponent>(ent);
        
        collided.CurrentItem = ent;
        Log.Debug($"{ent} was taken by {args.CollidedEntity}");
    }
}