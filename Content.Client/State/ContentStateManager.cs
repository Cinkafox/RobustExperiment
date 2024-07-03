using System.Linq;
using Content.Shared.IoC;
using Content.Shared.State;
using Robust.Client.State;
using Robust.Shared.Player;
using Robust.Shared.Reflection;

namespace Content.Client.State;

[IoCRegister(typeof(IContentStateManager))]
public sealed class ContentStateManager : SharedContentStateManager
{
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    
    public override void Initialize()
    {
        NetManager.RegisterNetMessage<SessionStateChangeMessage>(OnSessionStateChange);
    }

    public override void ChangeSessionState(ICommonSession session, Type type)
    {
        var state = _reflectionManager.GetAllChildren(type).First();
        
        Logger.Debug("CHANGE STATE: " + state.FullName);
        _stateManager.RequestStateChange(state);
    }
    
    private void OnSessionStateChange(SessionStateChangeMessage message)
    {
        var impl = _reflectionManager.GetType(message.Type);
        if (impl is null)
        {
            Logger.Error("Type is null");
            return;
        }
        
        ChangeSessionState(default!,impl);
    }
}