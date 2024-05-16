using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class AnchorControl : Control
{
    private LayoutContainer.LayoutPreset _layoutPreset;
    public LayoutContainer.LayoutPreset LayoutPreset { get => _layoutPreset;
        set
        {
            _layoutPreset = value;
            LayoutContainer.SetAnchorPreset(this,_layoutPreset);
            foreach (var child in Children)
            {
                LayoutContainer.SetAnchorPreset(child,_layoutPreset);
            }
        }
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);
        LayoutContainer.SetAnchorPreset(newChild,_layoutPreset);
    }
}