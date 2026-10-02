using Robust.Shared.Animations;

namespace Content.Shared.Animations.Data;

[DataDefinition]
public sealed partial class BodyAnimationTrack
{
    [DataField] public Type? ComponentType;
    [DataField] public AnimationInterpolationMode InterpolationMode = AnimationInterpolationMode.Cubic;
    [DataField] public string Property;
    [DataField] public string? Bone;
    [DataField] public BodyAnimationKeyContainer KeyFrames;
}