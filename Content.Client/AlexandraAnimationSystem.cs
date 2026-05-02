using Content.Client.Bone;
using Content.Shared.Bone;
using Content.Shared.Movement;
using Content.Shared.Transform;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client;

public sealed class AlexandraAnimationSystem: EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayerSystem = default!;
    [Dependency] private readonly BoneSystem _boneSystem = default!;
    [Dependency] private readonly Transform3dSystem _transform3dSystem = default!;
    
    
    public Animation Standby1 => new Animation()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.5f),
                }
            }
        }
    };
    
    public Animation Animation1 => new Animation()
    {
        Length = TimeSpan.FromSeconds(1),
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
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-40), (float)Angle.FromDegrees(-65)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(40), (float)Angle.FromDegrees(-65)), 0.25f),
                }
            }
        }
    };
    
    public Animation Standby2 => new Animation()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 0), (float)Angle.FromDegrees(-55)), 0.5f),
                }
            }
        }
    };
    
    public Animation Animation2 => new Animation()
    {
        Length = TimeSpan.FromSeconds(1),
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
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 0), (float)Angle.FromDegrees(-55)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 - 40), (float)Angle.FromDegrees(-65)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 0), (float)Angle.FromDegrees(-55)), 0.25f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(180 + 40), (float)Angle.FromDegrees(-65)), 0.25f),
                }
            }
        }
    };
    
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<BoneComponent, AnimationCompletedEvent>(OnEnd);
        SubscribeLocalEvent<AlexandraAnimationComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<AlexandraAnimationComponent, EntityStartMoveEvent>(OnMove);
        SubscribeLocalEvent<AlexandraAnimationComponent, EntityEndMoveEvent>(OnEndMove);
    }

    private void OnEndMove(Entity<AlexandraAnimationComponent> ent, ref EntityEndMoveEvent args)
    {
        Stop(ent);
    }

    private void OnMove(Entity<AlexandraAnimationComponent> ent, ref EntityStartMoveEvent args)
    {
        Play(ent);
    }

    private void OnInit(Entity<AlexandraAnimationComponent> ent, ref ComponentStartup args)
    {
        Stop(ent.Owner);
    }

    private void OnEnd(Entity<BoneComponent> ent, ref AnimationCompletedEvent args)
    {
        if(!TryComp<AlexandraAnimationComponent>(ent.Comp.Skeleton, out var anim) || !anim.DoAnimation)
            return;
        
        if(args.Key == "kadeem1") _animationPlayerSystem.Play(ent,Animation1 , "kadeem1");
        if(args.Key == "kadeem2") _animationPlayerSystem.Play(ent,Animation2 , "kadeem2");
    }

    private void Play(EntityUid uid)
    {
        Log.Info($"PLAY ANIMATION FOR {uid}");
        
        if(!_boneSystem.TryGetBone(uid, "shoulder1", out var shoulder1) ||
           !_boneSystem.TryGetBone(uid, "shoulder2", out var shoulder2))
            return;
        
        EnsureComp<AlexandraAnimationComponent>(uid).DoAnimation = true;
        
        _animationPlayerSystem.Stop(shoulder1, "standby1");
        _animationPlayerSystem.Stop(shoulder2, "standby2");
        
        _animationPlayerSystem.Play(shoulder1, Animation1, "kadeem1");
        _animationPlayerSystem.Play(shoulder2, Animation2, "kadeem2");
    }

    private void Stop(EntityUid uid)
    {
        Log.Info($"STOP ANIMATION FOR {uid}");
        
        if(!_boneSystem.TryGetBone(uid, "shoulder1", out var shoulder1) ||
           !_boneSystem.TryGetBone(uid, "shoulder2", out var shoulder2))
            return;
        
        EnsureComp<AlexandraAnimationComponent>(uid).DoAnimation = false;
        
        _animationPlayerSystem.Stop(shoulder1, "kadeem1");
        _animationPlayerSystem.Stop(shoulder2, "kadeem2");
        
        _animationPlayerSystem.Play(shoulder1, Standby1, "standby1");
        _animationPlayerSystem.Play(shoulder2, Standby2, "standby2");
    }
}