using Content.Shared.IoC;
using Content.Shared.State;
using Robust.Shared.Player;

namespace Content.Server.State;

[IoCRegister(typeof(IContentStateManager))]
public sealed class ContentStateManager : SharedContentStateManager
{
    public override void Initialize()
    {
        NetManager.RegisterNetMessage<SessionStateChangeMessage>();
    }

    public override void ChangeSessionState(ICommonSession session, Type type)
    {
        var message = new SessionStateChangeMessage()
        {
            Type = type.FullName!
        };
        
        NetManager.ServerSendMessage(message,session.Channel);
    }
}