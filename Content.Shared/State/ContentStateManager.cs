using Content.Shared.IoC;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.State;

public abstract class SharedContentStateManager : IContentStateManager, IInitializeBehavior
{
    [Dependency] protected readonly INetManager NetManager = default!;
    
    public abstract void Initialize();
    
    public abstract void ChangeSessionState(ICommonSession session, Type type);

    public void ChangeSessionState<T>(ICommonSession session)
    {
        ChangeSessionState(session,typeof(T));
    }
}

public interface IContentStateManager
{
    public void ChangeSessionState(ICommonSession session, Type type);
    public void ChangeSessionState<T>(ICommonSession session);
}

public sealed class SessionStateChangeMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public string Type { get; set; } = string.Empty; 
    
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Type = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Type);
    }
}