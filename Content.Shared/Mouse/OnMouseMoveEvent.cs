using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Mouse;

[Serializable, NetSerializable]
public sealed class OnMouseMoveEvent(Vector2 delta) : EntityEventArgs
{
    public Vector2 Delta = delta;
}