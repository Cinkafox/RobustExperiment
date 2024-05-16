using System.Linq;
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Shared.Animations;

namespace Content.Client.UserInterface.AnimationHelper;

public sealed class ValueAnimator<T> : Control 
{
    public TimeSpan Length;
        
    private readonly Action<T> _action;
        
    private readonly Guid _guid = Guid.NewGuid();
        
    public Animation? Animation { get; private set; }

    private AnimationTrackControlProperty? _track;
        
    public AnimationTrackControlProperty? Track
    {
        get => _track;
        set
        {
            if(value is null) return;
            _track = value;
            _track.Property = nameof(Value);
                
            Length = _track.KeyFrames.Aggregate(TimeSpan.Zero, 
                (span, frame) => span.Add(TimeSpan.FromSeconds(frame.KeyTime)));

            Animation = new Animation()
            {
                Length = Length, AnimationTracks = { _track }
            };
        }
    }

    [Animatable]
    public T Value
    {
        get => default!;
        set => _action.Invoke(value);
    }

    public ValueAnimator(Action<T> action, Control parent) 
    {
        _action = action;
        parent.AddChild(this);
    }

    public void PlayAnimation()
    {
        if(Animation is null) return;
        PlayAnimation(Animation,_guid.ToString());
    }

    public bool HasRunningAnimation()
    {
        return HasRunningAnimation(_guid.ToString());
    }
}