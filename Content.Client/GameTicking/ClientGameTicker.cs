using System.Linq;
using Content.Client.Game;
using Content.Shared.GameTicking;
using Robust.Client;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Player;

namespace Content.Client.GameTicking;

public sealed class ClientGameTicker : SharedGameTicker
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<SessionStateChangeRequiredEvent>(OnChangeStateRequired);
    }

    private void OnChangeStateRequired(SessionStateChangeRequiredEvent ev)
    {
        var impl = _reflectionManager.GetType(ev.Type);
        if (impl is null)
        {
            Logger.Error("Type is null");
            return;
        }
        
        ChangeState(impl);
    }

    public void StartSinglePlayer()
    {
        InitializeMap();
        AddSession(_playerManager.LocalSession!);
    }

    public override void ChangeSessionState<T>(ICommonSession session)
    {
        ChangeState(typeof(T));
    }

    private void ChangeState(Type imp)
    {
        var state = _reflectionManager.GetAllChildren(imp).First();
        
        Logger.Debug("CHANGE STATE: " + state.FullName);
        _stateManager.RequestStateChange(state);
    }
}