using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class ChatMessageEvent(NetEntity source, string text) : EntityEventArgs
{
    public NetEntity Source = source;
    public string Text = text;
}
