using Robust.Shared.Serialization;

namespace Content.Shared.Chat;

[Serializable, NetSerializable]
public sealed class ChatMessageRequest(string text) : EntityEventArgs
{
    public const int MaxLength = 200;

    public string Text = text;
}
