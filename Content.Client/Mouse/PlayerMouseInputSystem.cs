using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Mouse;
using Content.Shared.Movement;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Mouse;

public sealed partial class PlayerMouseInputSystem : EntitySystem
{
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private CameraMouseSystem _cameraMouse = default!;

    private const float AutoTurnSmoothing = 0.15f;
    private const uint HistoryLength = 300;

    private GameTick _lastSentTick = GameTick.Zero;
    private EntityUid? _lastSentEntity;
    private Vector3 _lastSentState = new(float.NaN);

    private readonly Dictionary<GameTick, LookState> _history = new();
    private readonly Queue<GameTick> _historyOrder = new();

    private readonly record struct LookState(Vector2 Look, float BodyYaw);

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(InputMoverSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_baseClient.RunLevel.IsInGameLike())
        {
            Reset();
            return;
        }

        var local = _playerManager.LocalEntity;
        if (local is null || !TryComp<MouseCameraMoveComponent>(local.Value, out var mouse))
            return;

        if (_lastSentEntity != local)
        {
            _lastSentEntity = local;
            _lastSentTick = GameTick.Zero;
            _lastSentState = new Vector3(float.NaN);
            _history.Clear();
            _historyOrder.Clear();
        }

        if (!_timing.IsFirstTimePredicted)
        {
            if (_timing.InPrediction && _history.TryGetValue(_timing.CurTick, out var historical))
            {
                _cameraMouse.ApplyLook(local.Value, mouse, historical.Look, historical.BodyYaw,
                    updateCameraRotation: false);
            }

            return;
        }

        if (_timing.CurTick <= _lastSentTick)
            return;

        if (!TryComp<Transform3dComponent>(local.Value, out var transform) ||
            !TryComp<InputMoverComponent>(local.Value, out var inputMover))
        {
            return;
        }

        _lastSentTick = _timing.CurTick;

        AutoTurn(mouse, transform, inputMover);
        _cameraMouse.ApplyRotation(local.Value, mouse);

        var look = new Vector2((float)mouse.CameraRotation.Yaw, (float)mouse.CameraRotation.Pitch);
        var bodyYaw = (float)transform.LocalAngle.Yaw;
        var state = new Vector3(look.X, look.Y, bodyYaw);

        _history[_timing.CurTick] = new LookState(look, bodyYaw);
        _historyOrder.Enqueue(_timing.CurTick);
        CleanupHistory();

        if (state == _lastSentState)
            return;

        _lastSentState = state;
        RaiseNetworkEvent(new LookStateEvent(_timing.CurTick, look, bodyYaw));
    }

    private static void AutoTurn(MouseCameraMoveComponent mouse, Transform3dComponent transform,
        InputMoverComponent inputMover)
    {
        if (inputMover.IsMoving)
        {
            var deltaYaw = -mouse.CameraRotation.Yaw;
            deltaYaw *= AutoTurnSmoothing;
            mouse.CameraRotation = new EulerAngles(mouse.CameraRotation.Pitch,
                mouse.CameraRotation.Yaw + deltaYaw, 0);
            transform.LocalAngle = new EulerAngles(transform.LocalAngle.Pitch,
                transform.LocalAngle.Yaw - deltaYaw, transform.LocalAngle.Roll);
        }
        else
        {
            var deltaAngle = -mouse.CameraRotation.Yaw;

            if (deltaAngle > mouse.AutoTurnThreshold || deltaAngle < -mouse.AutoTurnThreshold)
            {
                deltaAngle *= AutoTurnSmoothing;
                mouse.CameraRotation = new EulerAngles(mouse.CameraRotation.Pitch,
                    mouse.CameraRotation.Yaw + deltaAngle, 0);
                transform.LocalAngle = new EulerAngles(transform.LocalAngle.Pitch,
                    transform.LocalAngle.Yaw - deltaAngle, transform.LocalAngle.Roll);
            }
        }
    }

    private void CleanupHistory()
    {
        var cutoff = _timing.CurTick.Value > HistoryLength
            ? _timing.CurTick - HistoryLength
            : GameTick.Zero;

        while (_historyOrder.Count > 0 && _historyOrder.Peek() < cutoff)
            _history.Remove(_historyOrder.Dequeue());
    }

    private void Reset()
    {
        _lastSentTick = GameTick.Zero;
        _lastSentEntity = null;
        _lastSentState = new Vector3(float.NaN);
        _history.Clear();
        _historyOrder.Clear();
    }
}
