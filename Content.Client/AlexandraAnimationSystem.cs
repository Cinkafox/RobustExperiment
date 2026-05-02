using Content.Client.Bone;
using Content.Shared.Bone;
using Content.Shared.Transform;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client;

public sealed class AlexandraAnimationSystem: EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayerSystem = default!;
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    
    public Animation Animation1 => new Animation()
    {
        Length = TimeSpan.FromSeconds(2),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(40), (float)Angle.FromDegrees(-65)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-40), (float)Angle.FromDegrees(-65)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(40), (float)Angle.FromDegrees(-65)), 0.5f),
                }
            }
        }
    };
    
    public Animation Animation2 => new Animation()
    {
        Length = TimeSpan.FromSeconds(2),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 40), (float)Angle.FromDegrees(-65)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 0), (float)Angle.FromDegrees(-55)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 - 40), (float)Angle.FromDegrees(-65)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 0), (float)Angle.FromDegrees(-55)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 40), (float)Angle.FromDegrees(-65)), 0.5f),
                }
            }
        }
    };
    
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<Transform3dComponent, AnimationCompletedEvent>(OnEnd);
        SubscribeLocalEvent<AlexandraAnimationComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(Entity<AlexandraAnimationComponent> ent, ref ComponentStartup args)
    {
        Play(ent.Owner);
    }

    private void OnEnd(Entity<Transform3dComponent> ent, ref AnimationCompletedEvent args)
    {
        if(args.Key == "kadeem1") _animationPlayerSystem.Play(ent,Animation1 , "kadeem1");
        if(args.Key == "kadeem2") _animationPlayerSystem.Play(ent,Animation2 , "kadeem2");
    }

    private void Play(EntityUid uid)
    {
        Log.Info($"PLAY ANIMATION FOR {uid}");
        
        if(!_boneSystem.TryGetBone(uid, "shoulder1", out var shoulder1) ||
           !_boneSystem.TryGetBone(uid, "shoulder2", out var shoulder2))
            return;
        
        _animationPlayerSystem.Play(shoulder1, Animation1, "kadeem1");
        _animationPlayerSystem.Play(shoulder2, Animation2, "kadeem2");
    }
}