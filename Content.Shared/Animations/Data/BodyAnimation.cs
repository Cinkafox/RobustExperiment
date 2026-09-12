namespace Content.Shared.Animations.Data;

[DataDefinition]
public sealed partial class BodyAnimation
{
    [DataField] public TimeSpan Length;
    [DataField] public bool Looped;
    [DataField] public List<BodyAnimationTrack> Tracks;
}