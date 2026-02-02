using Content.Shared.Bone;
using Content.Shared.Transform;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using BoneComponent = Content.Shared.Bone.BoneComponent;
using SkeletonComponent = Content.Shared.Bone.SkeletonComponent;

namespace Content.Client;

public sealed class AlexandraAnimationSystem: EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayerSystem = default!;
    
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
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(210),(float)Angle.FromDegrees(80), (float)Angle.FromDegrees(180)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(180),(float)Angle.FromDegrees(60), (float)Angle.FromDegrees(180)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(160),(float)Angle.FromDegrees(80), (float)Angle.FromDegrees(180)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(180),(float)Angle.FromDegrees(60), (float)Angle.FromDegrees(180)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(210),(float)Angle.FromDegrees(80), (float)Angle.FromDegrees(180)), 0.5f),
                }
            }
        }
    };
    
    public Animation Animation2 = new Animation()
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
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(30),(float)Angle.FromDegrees(-80), 0), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-60), 0), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-20),(float)Angle.FromDegrees(-80), 0), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-60), 0), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(30),(float)Angle.FromDegrees(-80), 0), 0.5f),
                }
            }
        }
    };
    
    public Animation Animation3 = new Animation()
    {
        Length = TimeSpan.FromSeconds(5),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,0,0), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(359),0,0), 5f),
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
        if(args.Key == "aaa") _animationPlayerSystem.Play(ent,Animation3 , "aaa");
    }

    private void Play(EntityUid uid)
    {
        Log.Info($"PLAY ANIMATION FOR {uid}");
        _animationPlayerSystem.Play(uid, Animation3, "aaa");
   
        var boneEnt = Comp<SkeletonComponent>(uid).Root;
        var boneChild = Comp<BoneComponent>(boneEnt).Childs;
        var count = 0;
        
        foreach (var child in boneChild)
        {
            if (count % 2 == 1)
            {
                _animationPlayerSystem.Play(child, Animation1, "kadeem1");
            }
            else
            {
                _animationPlayerSystem.Play(child, Animation2, "kadeem2");
            }
            count++;
        }
    }
}