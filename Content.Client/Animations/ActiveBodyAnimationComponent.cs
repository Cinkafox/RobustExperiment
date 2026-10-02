using Content.Shared.Animations;
using Robust.Client.Animations;
using Robust.Shared.Prototypes;

namespace Content.Client.Animations;
 
[RegisterComponent]
public sealed partial class ActiveBodyAnimationComponent : Component, IActiveBodyAnimation
{
    [ViewVariables] public EntityUid MainUid { get; set; }
    [ViewVariables] public Animation CurrentAnimation { get; set; }
    [ViewVariables] public ProtoId<BodyAnimationPrototype> AnimationId { get; set; }
}

[RegisterComponent]
public sealed partial class ActiveLoopedBodyAnimationComponent : Component, IActiveBodyAnimation
{
    [ViewVariables] public EntityUid MainUid { get; set; }
    [ViewVariables] public Animation CurrentAnimation { get; set; }
    [ViewVariables] public ProtoId<BodyAnimationPrototype> AnimationId { get; set; }
}

public interface IActiveBodyAnimation
{
    public EntityUid MainUid { get; set; }
    public Animation CurrentAnimation { get; set; }
    public ProtoId<BodyAnimationPrototype> AnimationId { get; set; }
}