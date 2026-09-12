using Robust.Shared.Prototypes;

namespace Content.Shared.Animations;

[RegisterComponent]
public sealed partial class BodyAnimationComponent : Component
{
    [ViewVariables] public Dictionary<ProtoId<BodyAnimationPrototype>, TimeSpan> ActiveAnimations = [];
}