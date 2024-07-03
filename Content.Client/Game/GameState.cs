using Content.Client.GameTicking;
using Content.Client.UserInterface;
using Content.Client.Viewport.Controls;
using Content.Shared.States;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Game;

public sealed class GameState : UIState<GameScreen>, IGameState
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IBaseClient _baseClient = default!;
    
    public MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

    protected override void UIStartup()
    {
        _inputManager.KeyBindStateChanged += InputManagerOnKeyBindStateChanged;
    }

    protected override void UIShutdown()
    {
        _inputManager.KeyBindStateChanged -= InputManagerOnKeyBindStateChanged;
    }

    private void InputManagerOnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
    {
        // If there is no InputSystem, then there is nothing to forward to, and nothing to do here.
        if (!_entitySystemManager.TryGetEntitySystem(out InputSystem? inputSys))
            return;

        var kArgs = args.KeyEventArgs;
        var func = kArgs.Function;
        var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

        EntityCoordinates coordinates = default;
        if (args.Viewport is IViewportControl vp)
        {
            var mousePosWorld = vp.PixelToMap(kArgs.PointerLocation.Position);
            coordinates = _mapManager.TryFindGridAt(mousePosWorld, out _, out var grid)
                ? grid.MapToGrid(mousePosWorld)
                : EntityCoordinates.FromMap(_mapManager, mousePosWorld);
        }

        var message = new ClientFullInputCmdMessage(_timing.CurTick, _timing.TickFraction, funcId)
        {
            State = kArgs.State,
            Coordinates = coordinates,
            ScreenCoordinates = kArgs.PointerLocation,
            Uid = default
        }; 

        // client side command handlers will always be sent the local player session.
        var session = _playerManager.LocalSession;
        if (inputSys.HandleInputCommand(session, func, message)) kArgs.Handle();
    }
}