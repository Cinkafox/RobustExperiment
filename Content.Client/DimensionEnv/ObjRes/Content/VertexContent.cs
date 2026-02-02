using System.Globalization;

namespace Content.Client.DimensionEnv.ObjRes.Content;

public sealed class VertexContent : BaseContent
{
    public Vector4 Vertex;

    public VertexContent(string[] args, int argStart)
    {
        Vertex = new Vector4(
            float.Parse(args[argStart], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 1], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 2], CultureInfo.InvariantCulture.NumberFormat),
            GetWOrDefault(args, argStart)
        );
    }

    private float GetWOrDefault(string[] args, int argStart)
    {
        if (args.Length < argStart + 4) return 1f;
        return float.Parse(args[argStart + 3], CultureInfo.InvariantCulture.NumberFormat);
    }
}