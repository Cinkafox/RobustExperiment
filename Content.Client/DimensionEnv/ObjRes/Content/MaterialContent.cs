namespace Content.Client.DimensionEnv.ObjRes.Content;

public sealed class MaterialContent : BaseContent
{
    public string Material;

    public MaterialContent(string[] args, int argStart)
    {
        Material = args[argStart];
    }
}