using Robust.Shared.Serialization;

namespace Content.Shared.GameState;

[Serializable, NetSerializable]
public sealed class GameStateEvent(ContentGameState contentGameState) : EntityEventArgs
{
    public ContentGameState ContentGameState = contentGameState;
}