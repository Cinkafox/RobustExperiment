using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.AnimationHelper;

public sealed class AnchorPreset : Control
{

    private LayoutContainer.LayoutPreset _layoutPreset;
    public LayoutContainer.LayoutPreset LayoutPreset { get => _layoutPreset;
        set
        {
            _layoutPreset = value;
            if (Parent is not null)
            {
                LayoutContainer.SetAnchorPreset(Parent,_layoutPreset);
            }
        }
    }
    
    protected override void Parented(Control newParent)
    {
        LayoutContainer.SetAnchorPreset(newParent,_layoutPreset);
        base.Parented(newParent);
    }
}