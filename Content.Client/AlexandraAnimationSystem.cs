using Content.Client.Bone;
using Content.Client.DimensionEnv;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client;

public sealed class AlexandraAnimationSystem: EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayerSystem = default!;
    
    public static Animation Animation1 = new Animation()
    {
        Length = TimeSpan.FromSeconds(2),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "WorldRotAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(210),(float)Angle.FromDegrees(80)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(180),(float)Angle.FromDegrees(60)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(160),(float)Angle.FromDegrees(80)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(180),(float)Angle.FromDegrees(60)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(210),(float)Angle.FromDegrees(80)), 0.5f),
                }
            }
        }
    };
    
    public static Animation Animation2 = new Animation()
    {
        Length = TimeSpan.FromSeconds(2),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "WorldRotAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(30),(float)Angle.FromDegrees(-80)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-60)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(-20),(float)Angle.FromDegrees(-80)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(0),(float)Angle.FromDegrees(-60)), 0.5f),
                    new AnimationTrackProperty.KeyFrame(new Vector3(0,(float)Angle.FromDegrees(30),(float)Angle.FromDegrees(-80)), 0.5f),
                }
            }
        }
    };
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<BoneComponent, AnimationCompletedEvent>(OnEnd);
    }

    private void OnEnd(Entity<BoneComponent> ent, ref AnimationCompletedEvent args)
    {
        if(args.Key == "kadeem1") _animationPlayerSystem.Play(ent,Animation1 , "kadeem1");
        if(args.Key == "kadeem2") _animationPlayerSystem.Play(ent,Animation2 , "kadeem2");
    }

    public void Play(EntityUid uid)
    {
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