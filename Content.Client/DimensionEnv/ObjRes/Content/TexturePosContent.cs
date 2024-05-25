using System.Globalization;
using System.Numerics;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class TexturePosContent : BaseContent
{
    public Vector2 TexturePos;

    public TexturePosContent(string[] args, int argStart)
    {
        TexturePos = new Vector2(
            float.Parse(args[argStart], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 1], CultureInfo.InvariantCulture.NumberFormat)
        );
    }
}