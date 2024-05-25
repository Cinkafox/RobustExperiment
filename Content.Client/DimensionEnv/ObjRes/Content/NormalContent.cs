using System.Globalization;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class NormalContent : BaseContent
{
    public Vector3 Normal;

    public NormalContent(string[] args, int argStart)
    {
        Normal = new Vector3(
            float.Parse(args[argStart], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 1], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 2], CultureInfo.InvariantCulture.NumberFormat)
        );
    }
}