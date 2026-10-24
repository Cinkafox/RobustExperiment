using Robust.Shared.Serialization;

namespace Content.Shared.GameState;

[Serializable, NetSerializable]
public enum ContentGameState
{
    None,
    InGame
}