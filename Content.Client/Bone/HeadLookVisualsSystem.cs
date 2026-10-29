using Content.Shared.Bone;
using Content.Shared.Camera;
using Content.Shared.Transform;
using Robust.Client.Player;

namespace Content.Client.Bone;

public sealed partial class HeadLookVisualsSystem : EntitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private BoneSystem _boneSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(BoneSkinningSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var local = _playerManager.LocalEntity;
        var query = EntityQueryEnumerator<HeadLookComponent, FollowCameraCreateComponent>();

        while (query.MoveNext(out var uid, out var headLook, out var followCamera))
        {
            if (uid == local)
                continue;

            if (!_boneSystem.TryGetBone(uid, followCamera.Bone, out var bone))
                continue;

            if (!TryComp<Transform3dComponent>(bone, out var transform))
                continue;

            transform.LocalAngle = headLook.Rotation;
        }
    }
}
