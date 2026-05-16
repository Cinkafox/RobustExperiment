using Content.Shared.Bone;
using Content.Shared.Input;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Items;

public sealed class ItemsSystem : EntitySystem
{
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3dSystem = default!;
    [Dependency] private readonly RigidBodySystem _rigidBodySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<CollectibleComponent, CollideObjectEvent>(OnCollide);
        
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.PlayerDropItemAction, new ItemDropKeyHandler(this))
            .Register<ItemsSystem>();
    }

    private void OnCollide(Entity<CollectibleComponent> ent, ref CollideObjectEvent args)
    {
        if(ent.Comp.IsTaken || 
           ent.Comp.CollideDelay > _gameTiming.CurTime ||
           !TryComp<ItemCollectorComponent>(args.CollidedEntity, out var collided) || 
           !collided.IsEmpty)
            return;
        
        args.Cancel();
        
        Take(ent.Owner, args.CollidedEntity.Owner);
    }

    public void Take(Entity<CollectibleComponent?> ent, Entity<ItemCollectorComponent?> collector)
    {
        if(!Resolve(ent, ref ent.Comp) || 
           !Resolve(collector, ref collector.Comp) ||
           !_boneSystem.TryGetBone(collector.Owner, collector.Comp.BoneName, out var bone))
            return;
        
        if(!collector.Comp.IsEmpty) 
            Drop(collector);
        
        ent.Comp.TakenBy = ent;
        
        _transform3dSystem.SetParent(ent, bone);
        var transform = Comp<Transform3dComponent>(ent);
        transform.LocalPosition = ent.Comp.Position;
        transform.LocalAngle = ent.Comp.Rotation;
        transform.LocalScale = ent.Comp.Scale;

        if (TryComp<RigidBodyComponent>(ent, out var rigidBody))
        {
            ent.Comp.TakenProperties = rigidBody.Properties;
            RemComp<RigidBodyComponent>(ent);
        }
        
        collector.Comp.CurrentItem = ent;
        Log.Debug($"{ent.Owner} was taken by {collector.Owner}");
    }

    public void Drop(Entity<ItemCollectorComponent?> collector)
    {
        if(!Resolve(collector, ref collector.Comp) || 
           collector.Comp.CurrentItem is null)
            return;

        var itemToDrop = collector.Comp.CurrentItem.Value;

        var collectorTransform = Comp<Transform3dComponent>(collector);
        var collectorBody = Comp<RigidBodyComponent>(collector);
        var transform = Comp<Transform3dComponent>(itemToDrop);
        var itemComp = Comp<CollectibleComponent>(itemToDrop);
        
        _transform3dSystem.SetParent(itemToDrop, collectorTransform.ParentUid);

        var translate = Matrix4Helpers.TransformVector(new Vector3(0, 1, 2), collectorTransform.LocalRotation);
        
        transform.LocalPosition = collectorTransform.LocalPosition + translate;
        transform.LocalRotation = collectorTransform.LocalRotation;

        if (itemComp.TakenProperties is not null)
        {
            var rigidBody = AddComp<RigidBodyComponent>(itemToDrop);
            rigidBody.Properties = itemComp.TakenProperties.Value;
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(itemToDrop, rigidBody), translate * rigidBody.Mass * 2 + collectorBody.LinearForce);
        }
        
        collector.Comp.CurrentItem = null;
        itemComp.TakenBy = null;
        itemComp.CollideDelay = _gameTiming.CurTime + TimeSpan.FromSeconds(0.5);
    }
}

public sealed class ItemDropKeyHandler(ItemsSystem itemsSystem) : InputCmdHandler
{
    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        if (message.State is BoundKeyState.Down && session is { AttachedEntity: not null }) 
            itemsSystem.Drop(session.AttachedEntity.Value);
        return true;
    }
}