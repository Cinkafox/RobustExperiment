using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Tile;

[Prototype("tile")]
public sealed partial class ContentTileDefinition : IPrototype, ITileDefinition
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField("sprite",required:true)] public SpriteSpecifier SpriteSpecifier { get; private set; } = default!;
    [DataField] public int EdgeSpritePriority { get; private set;}
    [DataField] public float Friction { get; private set;}
    [DataField] public byte Variants { get; private set; } = 1;
    [DataField] public string Name { get; set; } = string.Empty;
    public ResPath? Sprite { get; set; }
    public Dictionary<Direction, ResPath> EdgeSprites { get; set; } = new();
    public ushort TileId { get; private set; }
    public void AssignTileId(ushort id)
    {
        TileId = id;
    }
}