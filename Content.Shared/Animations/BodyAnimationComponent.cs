using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Animations;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodyAnimationComponent : Component
{
    [ViewVariables, AutoNetworkedField] public Dictionary<ProtoId<BodyAnimationPrototype>, TimeSpan> ActiveAnimations = [];
    [ViewVariables, AutoNetworkedField] public HashSet<ProtoId<BodyAnimationPrototype>> ActiveLoopedAnimation = [];
}