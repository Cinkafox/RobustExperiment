using Content.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Client.Animations;

[RegisterComponent]
public sealed partial class ClientBodyAnimComponent : Component
{
    [ViewVariables] public Dictionary<ProtoId<BodyAnimationPrototype>, TimeSpan> ActiveAnimations = [];
    [ViewVariables] public HashSet<ProtoId<BodyAnimationPrototype>> ActiveLoopedAnimation = [];
}