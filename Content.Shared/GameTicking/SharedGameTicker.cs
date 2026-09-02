using Content.Shared.Items;
using Content.Shared.Location;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract class SharedGameTicker : EntitySystem
{
    [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
    [Dependency] private readonly LocationSystem _locationSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3DSystem = default!;
    [Dependency] private readonly ConstraintSystem _constraintSystem = default!;
    
    public void InitializeGame()
    {
        _locationSystem.LoadLocation("default");
        TestSpawnThinks(new Vector3(4,5,0), 1.75f, 3);
        TestSpawnThinks(new Vector3(4,5,2), 1.75f, 3);
    }

    public void TestSpawnThinks(Vector3 position, float chainLength, int chainCount)
    {
        var staticThink = Spawn("sword");
        var physComp = EnsureComp<RigidBodyComponent>(staticThink);
        physComp.PhysType = PhysType.Static;
      
        _transform3DSystem.SetParent(staticThink, _locationSystem.MapUid);
        _transform3DSystem.SetWorldPosition(staticThink, position);
        
        var downRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI); 
        _transform3DSystem.SetWorldRotation(staticThink, downRotation);

        var parent = staticThink;
        for (var i = 0; i < chainCount; i++)
        {
            RemComp<CollectibleComponent>(parent);
            parent = SpawnChain(parent, chainLength);
        }
        
        RemComp<CollectibleComponent>(parent);
    }

    private EntityUid SpawnChain(EntityUid parent, float linkLength)
    {
        var chainThink = Spawn("sword");
        var halfLength = linkLength / 2f;
    
        var localAnchorA = new Vector3(0, -halfLength, 0); 
        var localAnchorB = new Vector3(0,  halfLength, 0); 
        
        var parentPos = _transform3DSystem.GetWorldPosition(parent);
        var parentRot = _transform3DSystem.GetWorldRotation(parent); 
        
        var parentBottomWorld = parentPos + Vector3.Transform(localAnchorA, parentRot);

        _transform3DSystem.SetParent(chainThink, _locationSystem.MapUid);
        _transform3DSystem.SetWorldRotation(chainThink, parentRot);
        
        var childInitialPos = parentBottomWorld - Vector3.Transform(localAnchorB, parentRot);
        _transform3DSystem.SetWorldPosition(chainThink, childInitialPos);
        
        _constraintSystem.AddConstraint(chainThink, parent, new PointToPointConstraint()
        {
            LocalAnchorA = localAnchorA,
            LocalAnchorB = localAnchorB,
        });
    
        var rb = EnsureComp<RigidBodyComponent>(chainThink);
        rb.Density = 65f;
    
        return chainThink;
    }

    public void AttachSession(ICommonSession session)
    {
        _locationSystem.AttachSession(session);
    }
}