using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Mouse;

[Serializable, NetSerializable]
public sealed class LookStateEvent(GameTick tick, Vector2 look, float bodyYaw) : EntityEventArgs
{
    public GameTick Tick = tick;
    public Vector2 Look = look;
    public float BodyYaw = bodyYaw;
}
