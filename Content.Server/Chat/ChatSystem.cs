using Content.Shared.Chat;

namespace Content.Server.Chat;

public sealed partial class ChatSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<ChatMessageRequest>(OnChatMessageRequest);
    }

    private void OnChatMessageRequest(ChatMessageRequest message, EntitySessionEventArgs args)
    {
        var source = args.SenderSession.AttachedEntity;

        if (source is null || !Exists(source.Value))
            return;

        var text = (message.Text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();

        if (text.Length == 0)
            return;

        if (text.Length > ChatMessageRequest.MaxLength)
            text = text[..ChatMessageRequest.MaxLength];

        RaiseNetworkEvent(new ChatMessageEvent(GetNetEntity(source.Value), text));
    }
}
