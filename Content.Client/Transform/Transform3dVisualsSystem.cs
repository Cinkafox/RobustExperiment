using System.Numerics;
using Content.Client.DimensionEnv;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Timing;

namespace Content.Client.Transform;

public sealed partial class Transform3dVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RigidBodySystem _rigidBodySystem = default!;

    private readonly Dictionary<EntityUid, VisualPose> _poses = new();
    private GameTick _lastTick;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var tick = _timing.CurTick;
        var tickChanged = tick != _lastTick;
        _lastTick = tick;

        if (!tickChanged)
            return;

        var query = EntityQueryEnumerator<Transform3dComponent, ModelComponent>();
        while (query.MoveNext(out var uid, out var xform, out _))
        {
            // Locally predicted bodies are rendered at their predicted pose.
            if (ShouldPredictVisual(uid, xform))
            {
                _poses.Remove(uid);
                continue;
            }

            var current = new Pose(xform.WorldPosition, xform.WorldRotation, xform.WorldScale);

            if (_poses.TryGetValue(uid, out var pose))
            {
                pose.Previous = pose.Current;
                pose.Current = current;
                _poses[uid] = pose;
            }
            else
            {
                _poses[uid] = new VisualPose(current, current);
            }
        }
    }

    public Matrix4x4 GetWorldMatrix(EntityUid uid, Transform3dComponent xform)
    {
        if (!_poses.TryGetValue(uid, out var pose) || ShouldPredictVisual(uid, xform))
            return xform.WorldMatrix;

        var previous = pose.Previous.Rotation;
        var current = pose.Current.Rotation;

        if (Quaternion.Dot(previous, current) < 0f)
            current = Quaternion.Negate(current);

        var alpha = Math.Clamp(_timing.TickPhase, 0f, 1f);

        var position = Vector3.Lerp(pose.Previous.Position, pose.Current.Position, alpha);
        var rotation = Quaternion.Slerp(previous, current, alpha);
        var scale = Vector3.Lerp(pose.Previous.Scale, pose.Current.Scale, alpha);

        return Matrix4Helpers.CreateTransform(position, rotation, scale);
    }

    private bool ShouldPredictVisual(EntityUid uid, Transform3dComponent xform)
    {
        if (_rigidBodySystem.ShouldPredict(uid))
            return true;

        // Entities attached to a predicted entity (e.g. items held in a hand bone)
        // must follow its locally predicted pose instead of being network-interpolated,
        // otherwise they lag behind and jitter relative to the predicted parent.
        var parent = xform.ParentUid;
        while (parent.IsValid())
        {
            if (_rigidBodySystem.ShouldPredict(parent))
                return true;

            if (!TryComp<Transform3dComponent>(parent, out var parentXform))
                break;

            parent = parentXform.ParentUid;
        }

        return false;
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent ev)
    {
        _poses.Remove(ev.Entity.Owner);
    }

    private readonly record struct Pose(Vector3 Position, Quaternion Rotation, Vector3 Scale);

    private struct VisualPose
    {
        public Pose Previous;
        public Pose Current;

        public VisualPose(Pose previous, Pose current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
