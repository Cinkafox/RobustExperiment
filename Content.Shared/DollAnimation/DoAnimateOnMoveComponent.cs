using Content.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Shared.DollAnimation;

[RegisterComponent]
public sealed partial class DoAnimateOnMoveComponent : Component
{
    [DataField] public ProtoId<BodyAnimationPrototype> OnMove;
    [DataField] public ProtoId<BodyAnimationPrototype> OnStandby;
    [DataField] public ProtoId<BodyAnimationPrototype> OnJump;
}