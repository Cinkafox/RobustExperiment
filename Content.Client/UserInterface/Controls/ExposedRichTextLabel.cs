using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

public sealed class ExposedRichTextLabel : RichTextLabel
{
    public string? Text
    {
        get => GetMessage();
        set
        {
            var m = new FormattedMessage();
            m.AddMarkup(value!);
            SetMessage(m);
        }
    }
}