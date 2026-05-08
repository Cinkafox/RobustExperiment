using Content.Shared.CollideUtil;
using Content.Shared.Health;
using Content.Shared.Items;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapon;

public sealed class WeaponSystem : EntitySystem
{
    [Dependency] private readonly HealthSystem _healthSystem = default!;
    [Dependency] private readonly RigidBodySystem _rigidBodySystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<WeaponComponent, ItemUseEvent>(OnUse);
        SubscribeLocalEvent<ActiveDamageAreaComponent, CollideObjectEvent>(OnCollide, [typeof(DeleteOnCollideComponent)]);
    }

    private void OnCollide(Entity<ActiveDamageAreaComponent> ent, ref CollideObjectEvent args)
    {
        if (args.CollidedEntity.Owner == ent.Comp.DamageSource)
        {
            args.Cancel();
            return;
        }
        
        if (TryComp<HealthComponent>(args.CollidedEntity, out var health) && !health.IsDead)
        {
            _healthSystem.ChangeHealth(new Entity<HealthComponent?>(args.CollidedEntity, health), - ent.Comp.Damage);
            args.Cancel();
        }
    }

    private void OnUse(Entity<WeaponComponent> ent, ref ItemUseEvent args)
    {
        var userTransform = EnsureComp<Transform3dComponent>(args.UsedBy);
        var position = userTransform.WorldPosition + 
                       Matrix4Helpers.TransformVector(ent.Comp.Translation, userTransform.WorldRotation);
        
        var damageArea = CreateDamageArea(args.UsedBy, position, ent.Comp.DamageAreaId);
        
        if (ent.Comp.Impulse != Vector3.Zero && 
            TryComp<RigidBodyComponent>(damageArea, out var rigidBody))
        {
            var impulse = Matrix4Helpers.TransformVector(ent.Comp.Impulse, userTransform.WorldRotation) * rigidBody.Mass;
            _rigidBodySystem.ApplyForce(new Entity<RigidBodyComponent>(damageArea, rigidBody), impulse);
        }
    }

    private EntityUid CreateDamageArea(EntityUid source, Vector3 position, EntProtoId areaProto)
    {
        var ent = Spawn(areaProto);
        var parent = Comp<Transform3dComponent>(source).ParentUid;
        var transform = EnsureComp<Transform3dComponent>(ent);
        var activeArea = EnsureComp<ActiveDamageAreaComponent>(ent);
        
        activeArea.DamageSource = source;
        transform.ParentUid = parent;
        transform.WorldPosition = position;

        return ent;
    }
}