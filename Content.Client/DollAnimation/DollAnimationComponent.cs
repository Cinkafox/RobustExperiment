using Robust.Client.Animations;

namespace Content.Client.DollAnimation;

[RegisterComponent]
public sealed partial class DollAnimationComponent : Component
{
    public Dictionary<string, AnimationProperty> RegisteredAnimations = [];
    
    public Dictionary<string, string> CurrentBoneLoopedAnimations = [];
    public Dictionary<string, string> CurrentBoneAnimations = [];
}

public record struct AnimationProperty(string BoneName, Animation Value, bool Looped);

[RegisterComponent]
public sealed partial class BoneOnLoopAnimationComponent : Component
{
    [ViewVariables] public EntityUid BoneOwner;
}

[RegisterComponent]
public sealed partial class BoneOnAnimationComponent : Component
{
    [ViewVariables] public EntityUid BoneOwner;
}