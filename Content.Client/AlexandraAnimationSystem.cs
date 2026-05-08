using Content.Client.DollAnimation;
using Content.Shared.Bone;
using Content.Shared.Movement;
using Content.Shared.Transform;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Client;

// TODO: Заменить прототипом всю эту мишуру.
public sealed class AlexandraAnimationSystem: EntitySystem
{
    [Dependency] private readonly DollAnimationSystem _dollAnimationSystem = default!;
    
    public Animation PistolFire => new Animation()
    {
        Length = TimeSpan.FromSeconds(15),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-90),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-90)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-95),(float)Angle.FromDegrees(5), (float)Angle.FromDegrees(-95)), 0.025f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-90),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-90)), 0.150f),
                }
            }
        }
    };
    
    public Animation PistolHold => new Animation()
    {
        Length = TimeSpan.FromSeconds(15),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-90),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-90)), 0f),
                }
            }
        }
    };
    
    public Animation WhoshAnim => new Animation()
    {
        Length = TimeSpan.FromSeconds(0.65),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(Transform3dComponent),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                Property = "LocalAngleAnim",
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-45),(float)Angle.FromDegrees(5), (float)Angle.FromDegrees(-55)), 0.15f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(-90),(float)Angle.FromDegrees(-45), (float)Angle.FromDegrees(-55)), 0.15f),
                    new AnimationTrackProperty.KeyFrame(new Vector3((float)Angle.FromDegrees(0),(float)Angle.FromDegrees(0), (float)Angle.FromDegrees(-55)), 0.35f)
                }
            }
        }
    };
    
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
        
        SubscribeLocalEvent<AlexandraAnimationComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<AlexandraAnimationComponent, EntityStartMoveEvent>(OnMove);
        SubscribeLocalEvent<AlexandraAnimationComponent, EntityEndMoveEvent>(OnEndMove);
    }

    private void OnEndMove(Entity<AlexandraAnimationComponent> ent, ref EntityEndMoveEvent args)
    {
        _dollAnimationSystem.PlayAnimation(ent.Owner, "standby1");
        _dollAnimationSystem.PlayAnimation(ent.Owner, "standby2");
    }

    private void OnMove(Entity<AlexandraAnimationComponent> ent, ref EntityStartMoveEvent args)
    {
        _dollAnimationSystem.PlayAnimation(ent.Owner, "walk1");
        _dollAnimationSystem.PlayAnimation(ent.Owner, "walk2");
    }

    private void OnInit(Entity<AlexandraAnimationComponent> ent, ref ComponentStartup args)
    {
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "walk1", new AnimationProperty("shoulder1", Animation1, true));
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "walk2", new AnimationProperty("shoulder2", Animation2, true));
       
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "standby1", new AnimationProperty("shoulder1", Standby1, true));
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "standby2", new AnimationProperty("shoulder2", Standby2, true));
       
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "whoosh", new AnimationProperty("shoulder1", WhoshAnim, false));
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "pistolhold", new AnimationProperty("shoulder1", PistolHold, false));
       _dollAnimationSystem.RegisterAnimation(ent.Owner, "pistolfire", new AnimationProperty("shoulder1", PistolFire, false));

       _dollAnimationSystem.PlayAnimation(ent.Owner, "standby1");
       _dollAnimationSystem.PlayAnimation(ent.Owner, "standby2");
    }
}