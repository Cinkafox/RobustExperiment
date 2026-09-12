using Robust.Client.Animations;

namespace Content.Client.Animations;
 
[RegisterComponent]
public sealed partial class ActiveBodyAnimationComponent: Component
{
    [ViewVariables] public EntityUid MainUid;
    [ViewVariables] public Animation CurrentAnimation;
    [ViewVariables] public bool IsLooped;
}