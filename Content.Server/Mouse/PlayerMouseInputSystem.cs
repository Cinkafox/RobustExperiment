using Content.Shared.Camera;
using Content.Shared.Mouse;
using Content.Shared.Movement;
using Robust.Shared.Timing;

namespace Content.Server.Mouse;

public sealed partial class PlayerMouseInputSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CameraMouseSystem _cameraMouse = default!;

    private const uint MaxLookLeadTicks = 120;

    private readonly Dictionary<EntityUid, Queue<LookStateEvent>> _pending = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<LookStateEvent>(OnLookState);
        UpdatesBefore.Add(typeof(InputMoverSystem));
    }

    private void OnLookState(LookStateEvent ev, EntitySessionEventArgs args)
    {
        var source = args.SenderSession.AttachedEntity;
        if (source is null || !Exists(source.Value))
            return;

        if (!TryComp<MouseCameraMoveComponent>(source.Value, out _))
            return;

        if (ev.Tick > _timing.CurTick + MaxLookLeadTicks)
            return;

        if (!_pending.TryGetValue(source.Value, out var queue))
            _pending[source.Value] = queue = new Queue<LookStateEvent>();

        queue.Enqueue(ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count == 0)
            return;

        _toRemove.Clear();

        foreach (var (uid, queue) in _pending)
        {
            if (!Exists(uid) || !TryComp<MouseCameraMoveComponent>(uid, out var mouse))
            {
                _toRemove.Add(uid);
                continue;
            }

            LookStateEvent? latest = null;
            while (queue.Count > 0 && queue.Peek().Tick <= _timing.CurTick)
                latest = queue.Dequeue();

            if (latest == null)
                continue;

            var rotation = _cameraMouse.ApplyLook(uid, mouse, latest.Look, latest.BodyYaw);
            if (rotation is not { } headRotation)
                continue;

            var headLook = EnsureComp<HeadLookComponent>(uid);
            headLook.Rotation = headRotation;
            Dirty(uid, headLook);
        }

        foreach (var uid in _toRemove)
            _pending.Remove(uid);
    }
}
