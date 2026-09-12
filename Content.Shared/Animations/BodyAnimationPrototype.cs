using Robust.Shared.Prototypes;

namespace Content.Shared.Animations;

[Prototype]
public sealed partial class BodyAnimationPrototype: IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public Data.BodyAnimation Animation = default!;
}