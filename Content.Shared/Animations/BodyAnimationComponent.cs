using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Animations;

[RegisterComponent, NetworkedComponent]
public sealed partial class BodyAnimationComponent : Component
{
    [ViewVariables] public Dictionary<ProtoId<BodyAnimationPrototype>, TimeSpan> ActiveAnimations = [];
    [ViewVariables] public HashSet<ProtoId<BodyAnimationPrototype>> ActiveLoopedAnimation = [];
}