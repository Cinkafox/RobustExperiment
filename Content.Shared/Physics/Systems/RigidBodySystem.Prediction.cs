using Content.Shared.Car;
using Content.Shared.Physics.Components;
using Content.Shared.Physics.Data;
using Content.Shared.Transform;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    private HashSet<EntityUid> _predictedBodies = new();
    private HashSet<EntityUid> _nextPredictedBodies = new();
    private readonly HashSet<EntityUid> _predictionRoots = new();
    private readonly Queue<EntityUid> _predictionQueue = new();
    private readonly Dictionary<EntityUid, List<EntityUid>> _predictionAdjacency = new();

    private void InitializePrediction()
    {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        RebuildPredictionSet();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        RebuildPredictionSet();
    }

    private bool IsSimulationAuthority => _netManager.IsServer || !_netManager.IsRunning;

    public bool ShouldPredict(EntityUid uid)
    {
        if (IsSimulationAuthority)
            return true;

        return _predictionRoots.Contains(uid) || _predictedBodies.Contains(uid);
    }

    private void RebuildPredictionSet()
    {
        if (IsSimulationAuthority)
            return;

        _nextPredictedBodies.Clear();
        _predictionQueue.Clear();
        _predictionAdjacency.Clear();
        _predictionRoots.Clear();

        var localEntity = _playerManager.LocalEntity;
        if (localEntity is { } player)
        {
            _predictionRoots.Add(player);

            if (TryComp<PlayerActiveCarRemoteControlComponent>(player, out var remoteControl) &&
                remoteControl.Car.IsValid())
            {
                _predictionRoots.Add(remoteControl.Car);
            }
        }

        BuildConstraintAdjacency();
        BuildContactAdjacency();

        foreach (var root in _predictionRoots)
        {
            AddPredicted(root);
        }

        while (_predictionQueue.TryDequeue(out var uid))
        {
            if (!_predictionAdjacency.TryGetValue(uid, out var neighbours))
                continue;

            foreach (var neighbour in neighbours)
            {
                AddPredicted(neighbour);
            }
        }

        (_predictedBodies, _nextPredictedBodies) = (_nextPredictedBodies, _predictedBodies);
    }

    private void BuildConstraintAdjacency()
    {
        var query = AllEntityQuery<RigidBodyComponent, ConstraintComponent>();
        while (query.MoveNext(out var uid, out var body, out var constraints))
        {
            if (body.PhysType != PhysType.Dynamic)
                continue;

            foreach (var (otherUid, _) in constraints.Constraints)
            {
                if (!TryComp<RigidBodyComponent>(otherUid, out var otherBody) ||
                    otherBody.PhysType != PhysType.Dynamic)
                {
                    continue;
                }

                AddPredictionEdge(uid, otherUid);
            }
        }
    }

    private void BuildContactAdjacency()
    {
        foreach (var contact in _contacts)
        {
            var bodyA = contact.BodyA.Comp;
            var bodyB = contact.BodyB.Comp;

            if (bodyA.PhysType != PhysType.Dynamic || bodyB.PhysType != PhysType.Dynamic)
                continue;

            AddPredictionEdge(contact.BodyA.Owner, contact.BodyB.Owner);
        }
    }

    private void AddPredictionEdge(EntityUid a, EntityUid b)
    {
        AddPredictionNeighbour(a, b);
        AddPredictionNeighbour(b, a);
    }

    private void AddPredictionNeighbour(EntityUid from, EntityUid to)
    {
        if (!_predictionAdjacency.TryGetValue(from, out var neighbours))
        {
            neighbours = new List<EntityUid>(2);
            _predictionAdjacency[from] = neighbours;
        }

        neighbours.Add(to);
    }

    private void AddPredicted(EntityUid uid)
    {
        if (!TryComp<RigidBodyComponent>(uid, out var body) || body.PhysType != PhysType.Dynamic)
            return;

        if (_nextPredictedBodies.Add(uid))
            _predictionQueue.Enqueue(uid);
    }

    private void PrepareNetSnapshots()
    {
        var query = AllEntityQuery<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            var predicted = ShouldPredict(uid);

            if (predicted && (!body.WasPredicted || !body.HasNetSnapshot))
                CaptureNetSnapshot(body, xform);

            body.WasPredicted = predicted;
        }
    }

    private static void CaptureNetSnapshot(RigidBodyComponent body, Transform3dComponent xform)
    {
        body.LastNetLinearVelocity = body.LinearVelocity;
        body.LastNetAngularVelocity = body.AngularVelocity;
        body.LastNetPosition = xform.LocalPosition;
        body.LastNetRotation = xform.LocalRotation;
        body.HasNetSnapshot = true;
    }
}
