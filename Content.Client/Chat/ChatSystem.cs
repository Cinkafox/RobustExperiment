using Content.Shared.Chat;
using Robust.Shared.Timing;

namespace Content.Client.Chat;

public sealed partial class ChatSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan MessageLifetime = TimeSpan.FromSeconds(8);
    private const int MaxMessages = 64;

    private readonly List<ChatMessage> _messages = new();

    public IReadOnlyList<ChatMessage> Messages => _messages;

    public override void Initialize()
    {
        SubscribeNetworkEvent<ChatMessageEvent>(OnChatMessage);
    }

    private void OnChatMessage(ChatMessageEvent message)
    {
        var source = GetEntity(message.Source);
        var text = message.Text?.Trim() ?? string.Empty;

        if (text.Length == 0 || !Exists(source))
            return;

        _messages.Add(new ChatMessage(source, text, _timing.CurTime + MessageLifetime));

        if (_messages.Count > MaxMessages)
            _messages.RemoveRange(0, _messages.Count - MaxMessages);
    }

    public void SendMessage(string text)
    {
        text = text.Trim();

        if (text.Length == 0)
            return;

        if (text.Length > ChatMessageRequest.MaxLength)
            text = text[..ChatMessageRequest.MaxLength];

        RaiseNetworkEvent(new ChatMessageRequest(text));
    }

    public override void Update(float frameTime)
    {
        _messages.RemoveAll(message => message.ExpiresAt <= _timing.CurTime || !Exists(message.Source));
    }
}

public readonly record struct ChatMessage(EntityUid Source, string Text, TimeSpan ExpiresAt);
