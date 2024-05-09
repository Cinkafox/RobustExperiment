using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.AnimationHelper;

public sealed class ExposedRichTextLabel : RichTextLabel
{
    public string? Text
    {
        get
        {
            return GetMessage();
        }
        set
        {
            var m = new FormattedMessage();
            m.AddMarkup(value!);
            SetMessage(m);
        }
    }
}