using System.Globalization;

namespace Content.Client.DimensionEnv.ObjRes.Content;

public sealed class VertexContent : BaseContent
{
    public Vector3 Vertex;

    public VertexContent(string[] args, int argStart)
    {
        Vertex = new Vector3(
            float.Parse(args[argStart], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 1], CultureInfo.InvariantCulture.NumberFormat),
            float.Parse(args[argStart + 2], CultureInfo.InvariantCulture.NumberFormat)
        );
    }
}