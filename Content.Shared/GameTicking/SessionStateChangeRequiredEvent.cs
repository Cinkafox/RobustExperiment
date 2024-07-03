using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

[Serializable, NetSerializable]
public sealed class SessionStateChangeRequiredEvent : EntityEventArgs
{
    public string Type;
    public SessionStateChangeRequiredEvent(Type type)
    {
        Type = type.FullName!;
    }
}